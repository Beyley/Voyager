using Bunkum.RealmDatabase;
using Realms;
using Voyager.Gopher;

namespace Voyager.Database;

public class VoyagerDatabaseContext : RealmDatabaseContext
{
    public Realm Realm => this._realm;

    public Index AddIndex(List<GopherLine> lines, Uri uri, string? displayName, Index responseInfo)
    {
        Index index = new()
        {
            Data = default,
            Uri = uri.ToString(),
            Time = DateTimeOffset.UtcNow,
            DisplayName = displayName,
            ResponseTime = responseInfo.ResponseTime,
            MissingDirectoryTerminator = responseInfo.MissingDirectoryTerminator,
            HasBlankLine = responseInfo.HasBlankLine,
            LineMissingType = responseInfo.LineMissingType,
            Encoding = responseInfo.Encoding

        };
        lines.ForEach(l =>
        {
            if (!string.IsNullOrWhiteSpace(l.DisplayString))
                index.Items.Add(l);
        });
        this._realm.Add(index, true);
        return index;
    }

    public bool Crawled(string uri) => this._realm.Find<Index>(uri) != null;

    public IQueryable<Index> GetAllIndexedPages() => this._realm.All<Index>();
    public IQueryable<CrawledHost> GetAllCrawledHosts() => this._realm.All<CrawledHost>();

    public void AddQueuedSelector(QueuedSelector selector)
    {
        this._realm.Add(selector.DeepCopy(), true);
    }

    public void RemoveQueuedSelector(QueuedSelector selector)
    {
        this._realm.RemoveRange(this.GetAllQueuedSelectors().Where(s => s._Uri == selector._Uri));
    }

    public IQueryable<QueuedSelector> GetAllQueuedSelectors() => this._realm.All<QueuedSelector>();

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