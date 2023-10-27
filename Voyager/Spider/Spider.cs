using System.Collections.Concurrent;
using System.Net.Sockets;
using OneOf;
using Voyager.Database;
using Index = Voyager.Database.Index;

namespace Voyager.Spider;

public class Spider
{
    private readonly VoyagerDatabaseProvider _databaseProvider;

    //List of hosts, used so we dont have to do .Keys.ToArray()[i]
    private readonly List<string> _hosts = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<QueuedSelector>> _queues = new();
    private readonly List<Uri> _uris;

    public Spider(List<Uri> uris, VoyagerDatabaseProvider databaseProvider)
    {
        this._uris = uris;
        this._databaseProvider = databaseProvider;

        //Enqueue all URLs to crawl
        foreach (Uri uri in this._uris)
            this.AddToQueue(new QueuedSelector
            {
                Uri = uri
            }, databaseProvider.GetContext());
    }

    private void AddToQueue(QueuedSelector selector, VoyagerDatabaseContext database)
    {
        if (!this._queues.TryGetValue(selector.Uri.Host, out ConcurrentQueue<QueuedSelector>? queue))
        {
            this._queues[selector.Uri.Host] = new ConcurrentQueue<QueuedSelector>();
            queue = this._queues[selector.Uri.Host];
            lock (this._hosts)
            {
                this._hosts.Add(selector.Uri.Host);
            }
        }

        if (!database.Crawled(selector.Uri) && queue.All(q => q.Uri != selector.Uri))
        {
            Console.WriteLine($"Adding {selector.Uri} to queue");
            queue.Enqueue(selector);
        }
        else
        {
            Console.WriteLine($"Selector {selector.Uri} has already been crawled, not adding!");
        }
    }

    public void Start()
    {
        const int threadCount = 2;

        ThreadData[] threads = new ThreadData[threadCount];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new()
            {
                Thread = new Thread(this.ThreadRun),
                Run = true,
                Busy = false,
            };
            threads[i].Thread.Start(threads[i]);
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

        int i = 0;
        while (data.Run)
        {
            //Sleep for 10ms, lets not completely eat up the whole CPU!
            Thread.Sleep(10);

            i++;
            i %= this._queues.Count;

            string host;
            //Get a random host
            lock (this._hosts)
                host = this._hosts[i];
            ConcurrentQueue<QueuedSelector> queue = this._queues[host];

            data.Busy = true;
            //If we couldn't dequeue, try again
            if (!queue.TryDequeue(out QueuedSelector? selector))
            {
                data.Busy = false;
                continue;
            }

            Uri uri = selector.Uri;

            VoyagerDatabaseContext database = this._databaseProvider.GetContext();

            if (database.Crawled(uri))
            {
                Console.WriteLine($"Already crawled {uri}, skipping");
                data.Busy = false;
                continue;
            }

            Console.WriteLine($"Crawling URI {uri}");

            try
            {
                OneOf<byte[], List<GopherLine>, string> line = GopherClient.Transaction(uri.Host, (ushort)uri.Port, uri.AbsolutePath);

                line.Switch(
                    bytes => {},
                    gophermap =>
                    {
                        Index index = database.AddIndex(gophermap, uri, selector.DisplayName);

                        foreach (GopherLine line in gophermap)
                        {
                            if (line.Type == GopherType.Submenu)
                            {
                                try
                                {
                                    Uri uri1 = new Uri($"gopher://{line.Hostname}:{line.Port}{line.Selector}");
                                    this.AddToQueue(new QueuedSelector
                                    {
                                        Uri = uri1,
                                        DisplayName = line.DisplayString
                                    }, database);
                                }
                                catch
                                {
                                    //
                                }
                            }
                        }
                    },
                    text => {}
                );

                //Increment the amount of things we have crawled
                Interlocked.Increment(ref data.Crawled);
            }
            catch(TimeoutException ex)
            {
                Console.WriteLine($"Timeout! {ex}");
            }
            catch(SocketException ex)
            {
                Console.WriteLine($"Socket exception! {ex}");
            }
            catch(IOException ex)
            {
                Console.WriteLine(ex);
            }

            data.Busy = false;
        }
    }

    private class QueuedSelector
    {
        public string? DisplayName;
        public Uri Uri;
    }

    private class ThreadData
    {
        public int Crawled;
        public required bool Run;
        public required bool Busy;
        public required Thread Thread;
    }
}