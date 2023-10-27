using Realms;

namespace Voyager.Database;

public partial class QueuedSelector : IRealmObject
{
    [PrimaryKey]
    public string _Uri { get; set; }
    [Ignored]
    public Uri Uri
    {
        get => new Uri(_Uri); 
        set => _Uri = value.ToString();
    }
    public string? DisplayName { get; set; }

    public QueuedSelector DeepCopy()
    {
        return new QueuedSelector
        {
            _Uri = this._Uri,
            DisplayName = this.DisplayName
        };
    }
}