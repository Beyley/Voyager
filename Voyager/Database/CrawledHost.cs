using Realms;

namespace Voyager.Database;

public partial class CrawledHost : IRealmObject
{
    [PrimaryKey]
    public string Host { get; set; }
    public DateTimeOffset LastCrawl { get; set; }
    public bool Failed { get; set; }
}