using System.Collections.Concurrent;
using OneOf;
using Voyager.Database;
using Index = Voyager.Database.Index;

namespace Voyager.Spider;

public class Spider
{
    private readonly List<Uri> _uris;
    private readonly VoyagerDatabaseProvider _databaseProvider;

    class QueuedSelector
    {
        public Uri Uri;
        public string? DisplayName;
    }
    
    private readonly Dictionary<string, ConcurrentQueue<QueuedSelector>> _queues = new();

    private void AddToQueue(QueuedSelector selector)
    {
        Console.WriteLine($"Adding {selector.Uri} to selector");
        if (!this._queues.TryGetValue(selector.Uri.Host, out ConcurrentQueue<QueuedSelector>? queue))
        {
            this._queues[selector.Uri.Host] = new ConcurrentQueue<QueuedSelector>();
            queue = this._queues[selector.Uri.Host];
        }
        
        queue.Enqueue(selector);
    }
    
    public Spider(List<Uri> uris, VoyagerDatabaseProvider databaseProvider)
    {
        this._uris = uris;
        this._databaseProvider = databaseProvider;
        
        //Enqueue all URLs to crawl
        foreach (Uri uri in this._uris) this.AddToQueue(new QueuedSelector
        {
            Uri = uri,
        });
    }

    public void Start()
    {
        Thread[] threads = new Thread[48];
        for (int i = 0; i < threads.Length; i++)
        {
            threads[i] = new(ThreadRun);
            threads[i].Start();
            Thread.Sleep(2000);
        }
        
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        // foreach (Uri uri in this._uris)
        // {
        //     OneOf<byte[],List<GopherLine>,string> line = GopherClient.Transaction(uri.Host, (ushort)uri.Port, uri.AbsolutePath);
        //
        //     line.Switch(
        //         bytes => {},
        //         gophermap =>
        //         {
        //             Index index = this._databaseContext.AddIndex(gophermap, uri, null);
        //
        //             foreach (GopherLine line in index.Items)
        //             {
        //                 if (line.Type == GopherType.Submenu)
        //                 {
        //                     Uri subUri = new($"gopher://{line.Hostname}:{line.Port}{line.Selector}");
        //                     OneOf<byte[],List<GopherLine>,string> lines = GopherClient.Transaction(subUri.Host, (ushort)subUri.Port, subUri.AbsolutePath);
        //                     this._databaseContext.AddIndex(lines.AsT1, subUri, line.DisplayString);
        //                 }
        //             }
        //         },
        //         text => {}
        //     );
        // }
    }
    private void ThreadRun(object? obj)
    {
        int count = 0;
        
        int i = 0;
        while (this._queues.Count > 0 && count < 300)
        {
            i %= this._queues.Count;

            ConcurrentQueue<QueuedSelector>[] queues = this._queues.Values.ToArray();
            if (queues.All(q => q.IsEmpty)) break;

            string host = this._queues.Keys.ToArray()[i];
            ConcurrentQueue<QueuedSelector> queue = this._queues[host];

            if (!queue.TryDequeue(out QueuedSelector? selector))
            {
                i++;

                continue; 
            }

            Uri uri = selector.Uri;

            VoyagerDatabaseContext database = this._databaseProvider.GetContext();
            
            if (database.Crawled(uri))
            {
                Console.WriteLine($"Already crawled {uri}, skipping");
                i++;

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
                                this.AddToQueue(new QueuedSelector
                                {
                                    Uri = new Uri($"gopher://{line.Hostname}:{line.Port}{line.Selector}"),
                                    DisplayName = line.DisplayString,
                                });
                            }
                        }
                    },
                    text => {}
                );
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to crawl {uri} {ex}");
            }
            
            i++;

            count++;
        }
    }
}