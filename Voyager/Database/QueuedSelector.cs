using Realms;

namespace Voyager.Database;

public partial class QueuedSelector : IRealmObject
{
    [PrimaryKey]
    public string Uri { get; set; }
    public string SelectorName { get; set; }
}