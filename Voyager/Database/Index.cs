using Realms;
using Voyager.Spider;

namespace Voyager.Database;

public partial class Index : IRealmObject
{
    [PrimaryKey]
    public string Uri { get; set; }
    public IList<GopherLine> Items { get; }
    public DateTimeOffset Time { get; set; }
    [Indexed(IndexType.FullText)]
    public string? DisplayName { get; set; }
}