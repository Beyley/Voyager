using System.Collections.Concurrent;
using System.Net.Sockets;
using NotEnoughLogs;
using Realms;
using Voyager.Database;
using Voyager.Gopher;
using Index = Voyager.Database.Index;

namespace Voyager.Spider;

public class Spider
{
    private readonly List<string> _blacklistedHosts;
    private readonly VoyagerDatabaseProvider _databaseProvider;

    //List of hosts, used so we dont have to do .Keys.ToArray()[i]
    private readonly List<string> _hosts = new();
    private readonly Logger _logger;
    private readonly ConcurrentDictionary<string, ConcurrentQueue<QueuedSelector>> _queues = new();
    private readonly List<Uri> _uris;

    public Spider(List<Uri> uris, List<string> blacklistedHosts, VoyagerDatabaseProvider databaseProvider, Logger logger)
    {
        this._uris = uris;
        this._databaseProvider = databaseProvider;
        this._logger = logger;
        this._blacklistedHosts = blacklistedHosts;

        VoyagerDatabaseContext database = databaseProvider.GetContext();

        //Enqueue all URLs to crawl
        foreach (Uri uri in this._uris)
        {
            this.AddToQueue(new QueuedSelector
            {
                Uri = uri, 
                Reindex = true,
            }, database, false);
        }
    }

    private void AddToQueue(QueuedSelector selector, VoyagerDatabaseContext database, bool fillDatabase)
    {
        if (this._blacklistedHosts.Contains(selector.Uri.Host))
        {
            this._logger.LogInfo(VoyagerCategory.Spider, "Refusing to add blacklisted host {0}", selector.Uri.Host);
            return;
        }

        if (selector.Uri.Host.Contains("git", StringComparison.InvariantCultureIgnoreCase))
        {
            this._logger.LogInfo(VoyagerCategory.Spider, "Refusing to add git host");
            return;
        }

        if (selector.Uri.Host.Contains("scm", StringComparison.InvariantCultureIgnoreCase))
        {
            this._logger.LogInfo(VoyagerCategory.Spider, "Refusing to add scm host");
            return;
        }

        if (selector.Uri.Host.Contains("ftp", StringComparison.InvariantCultureIgnoreCase))
        {
            this._logger.LogInfo(VoyagerCategory.Spider, "Refusing to add ftp host");
            return;
        }

        if (selector.Uri.AbsolutePath.Contains("/git", StringComparison.InvariantCultureIgnoreCase))
        {
            this._logger.LogInfo(VoyagerCategory.Spider, "Refusing to add git selector");
            return;
        }

        if (selector.Uri.AbsolutePath.Contains("/scm", StringComparison.InvariantCultureIgnoreCase))
        {
            this._logger.LogInfo(VoyagerCategory.Spider, "Refusing to add scm selector");
            return;
        }

        if (selector.Uri.AbsolutePath.Contains("/commit/", StringComparison.InvariantCultureIgnoreCase))
        {
            this._logger.LogInfo(VoyagerCategory.Spider, "Refusing to add commit selector");
            return;
        }

        if (!this._queues.TryGetValue(selector.Uri.Host, out ConcurrentQueue<QueuedSelector>? queue))
        {
            this._queues[selector.Uri.Host] = new ConcurrentQueue<QueuedSelector>();
            queue = this._queues[selector.Uri.Host];
            lock (this._hosts)
            {
                this._hosts.Add(selector.Uri.Host);
            }
        }

        bool existsInQueue = false;
        foreach (QueuedSelector q in queue)
        {
            if (q._Uri != selector._Uri) continue;

            existsInQueue = true;
            break;
        }

        //If the selector is not in the queue, or it is a reindex, or it isnt a re-index, and we havent crawled it,
        if (!existsInQueue && (selector.Reindex || !database.Crawled(selector._Uri)))
        {
            //Enqueue it to be crawled
            this._logger.LogInfo(VoyagerCategory.Spider, "Adding {0} to queue", selector._Uri);
            queue.Enqueue(selector);
            if (fillDatabase)
                database.AddQueuedSelector(selector);
        }
    }

    public void Start()
    {
        // const int threadCount = 1;
        const int threadCount = 48;

        ThreadData[] threads = new ThreadData[threadCount];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new()
            {
                Thread = new Thread(this.ThreadRun),
                Run = true,
                Busy = false,
                Id = i
            };
            threads[i].Thread.Start(threads[i]);
        }

        VoyagerDatabaseContext database = this._databaseProvider.GetContext();
        IQueryable<QueuedSelector> selectors = database.GetAllQueuedSelectors();
        int queued = 0;
        int count = selectors.Count();
        foreach (QueuedSelector selector in selectors)
        {
            if (Console.KeyAvailable)
            {
                //If the enter key was hit
                if (Console.ReadKey().Key == ConsoleKey.Enter)
                    break;
            }

            this.AddToQueue(selector.DeepCopy(), database, false);
            this._logger.LogInfo(VoyagerCategory.Spider, "Filling queue from database... {0}/{1}", queued, count);
            queued++;
        }

        while (true)
        {
            bool exitEarly = false;
            if (Console.KeyAvailable)
            {
                //If the enter key was hit
                if (Console.ReadKey().Key == ConsoleKey.Enter) exitEarly = true;
            }

            //If all queues are empty and all threads are not busy
            if (this._queues.Values.All(q => q.IsEmpty) && threads.All(t => !t.Busy) || exitEarly)
            {
                //Stop all threads
                foreach (ThreadData data in threads)
                {
                    data.Run = false;
                }

                //Wait for all threads to stop
                foreach (ThreadData data in threads)
                {
                    data.Thread.Join();
                }
                break;
            }

            //Sleep for 100ms to not kill the CPU
            Thread.Sleep(100);
        }
    }

    private void ThreadRun(object? obj)
    {
        ThreadData data = (ThreadData)obj!;

        // int j = data.Id;
        while (data.Run)
        {
            //Sleep for 10ms, lets not completely eat up the whole CPU!
            Thread.Sleep(10);

            // j++;
            //Create an index that is offset by a random amount
            // int i = (j + Random.Shared.Next()) % this._queues.Count;
            int i = Random.Shared.Next(0, this._queues.Count);

            string host;
            //Get a random host
            lock (this._hosts)
            {
                host = this._hosts[i];
            }
            ConcurrentQueue<QueuedSelector> queue = this._queues[host];

            data.Busy = true;
            //If we couldn't dequeue, try again
            if (!queue.TryDequeue(out QueuedSelector? selector))
            {
                data.Busy = false;
                continue;
            }

            VoyagerDatabaseContext database = this._databaseProvider.GetContext();
            using Transaction transaction = database.Realm.BeginWrite();
            database.RemoveQueuedSelector(selector);
            transaction.Commit();

            Uri uri = selector.Uri;

            this._logger.LogInfo(VoyagerCategory.Spider, "Crawling URI {0}", uri);

            try
            {
                Index response = GopherClient.Transaction(uri.Host, (ushort)uri.Port, uri.AbsolutePath);

                response.Data.Switch(
                    bytes => {},
                    gophermap =>
                    {
                        using Transaction transaction = database.Realm.BeginWrite();
                        Index index = database.AddIndex(gophermap, uri, selector.DisplayName, response);

                        foreach (GopherLine submenuLine in index.Items)
                        {
                            if (submenuLine.Type == GopherType.Submenu)
                            {
                                try
                                {
                                    Uri uri1 = new($"gopher://{submenuLine.Hostname}:{submenuLine.Port}{submenuLine.Selector}");
                                    this.AddToQueue(new QueuedSelector
                                    {
                                        Uri = uri1,
                                        DisplayName = submenuLine.DisplayString, 
                                        Reindex = false,
                                    }, database, true);
                                }
                                catch
                                {
                                    //
                                }
                            }
                        }
                        transaction.Commit();
                    },
                    text => {}
                );

                //Increment the amount of things we have crawled
                Interlocked.Increment(ref data.Crawled);
                using Transaction transaction2 = database.Realm.BeginWrite();
                database.SetHostCrawlStatus(uri.Host, false);
                transaction2.Commit();
            }
            catch(TimeoutException ex)
            {
                this._logger.LogInfo(VoyagerCategory.Spider, "Timeout! {0}", ex);
                database.Realm.Write(() => database.SetHostCrawlStatus(uri.Host, true));
            }
            catch(SocketException ex)
            {
                this._logger.LogInfo(VoyagerCategory.Spider, "Socket exception! {0}", ex);
                database.Realm.Write(() => database.SetHostCrawlStatus(uri.Host, true));
            }
            catch(IOException ex)
            {
                this._logger.LogInfo(VoyagerCategory.Spider, ex.ToString());
                database.Realm.Write(() => database.SetHostCrawlStatus(uri.Host, true));
            }

            data.Busy = false;
        }
    }

    private class ThreadData
    {
        public required bool Busy;
        public int Crawled;
        public required int Id;
        public required bool Run;
        public required Thread Thread;
    }
}