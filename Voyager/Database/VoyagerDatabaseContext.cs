using Bunkum.RealmDatabase;
using Voyager.Spider;

namespace Voyager.Database;

public class VoyagerDatabaseContext : RealmDatabaseContext
{
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
        this._realm.Write(() => this._realm.Add(index, true));
        return index;
    }

    public bool Crawled(Uri uri)
    {
        return this._realm.All<Index>().Any(i => i.Uri == uri.ToString());
    }
}