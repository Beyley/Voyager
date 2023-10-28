using Bunkum.RealmDatabase;
using Realms;
using Voyager.Spider;

namespace Voyager.Database;

public class VoyagerDatabaseContext : RealmDatabaseContext
{
    public Realm Realm => _realm;
    
    public Index AddIndex(List<GopherLine> lines, Uri uri, string? displayName)
    {
        Index index = new()
        {
            Uri = uri.ToString(),
            Time = DateTimeOffset.UtcNow,
            DisplayName = displayName
        };
        lines.ForEach(l =>
        {
            if (!string.IsNullOrWhiteSpace(l.DisplayString))
                index.Items.Add(l);
        });
        this._realm.Add(index, true);
        return index;
    }

    public bool Crawled(string uri)
    {
        return this._realm.Find<Index>(uri.ToString()) != null;
    }

    public IQueryable<Index> GetAllIndexedPages() => this._realm.All<Index>();
    public IQueryable<CrawledHost> GetAllCrawledHosts() => this._realm.All<CrawledHost>();

    public void AddQueuedSelector(QueuedSelector selector)
    {
        this._realm.Add(new QueuedSelector
        {
            DisplayName = selector.DisplayName,
            Uri = selector.Uri
        }, true);
    }

    public void RemoveQueuedSelector(QueuedSelector selector)
    {
        this._realm.RemoveRange(this.GetAllQueuedSelectors().Where(s => s._Uri == selector._Uri));
    }

    public IQueryable<QueuedSelector> GetAllQueuedSelectors()
    {
        return this._realm.All<QueuedSelector>();
    }

    public void ClearQueuedSelectors()
    {
        this._realm.RemoveAll<QueuedSelector>();
    }

    public void SetHostCrawlStatus(string host, bool failed)
    {
        this._realm.Add(new CrawledHost
        {
            Host = host,
            LastCrawl = DateTimeOffset.UtcNow,
            Failed = failed
        }, true);
    }
    public IQueryable<Index> Search(string query)
    {
        return this._realm.All<Index>()
            .Where(index 
                => QueryMethods.FullTextSearch(index.DisplayName, query) 
                || QueryMethods.Contains(index.Uri, query, StringComparison.OrdinalIgnoreCase));
    }
}