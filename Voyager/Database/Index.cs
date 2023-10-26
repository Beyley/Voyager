using Bunkum.Protocols.Gopher.Responses;
using Realms;
using Voyager.Spider;

namespace Voyager.Database;

public partial class Index : IRealmObject
{
    [PrimaryKey]
    public string Uri { get; set; }
    public IList<GopherLine> Items { get; }
    public DateTimeOffset Time { get; set; }
    public string? DisplayName { get; set; }
}