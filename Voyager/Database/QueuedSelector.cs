using Realms;

namespace Voyager.Database;

public partial class QueuedSelector : IRealmObject
{
    [PrimaryKey]
    public string _Uri { get; set; }
    [Ignored]
    public Uri Uri
    {
        get => new(this._Uri);
        set => this._Uri = value.ToString();
    }
    public string? DisplayName { get; set; }
    public bool Reindex { get; set; }

    public QueuedSelector DeepCopy() => new()
    {
        _Uri = this._Uri,
        DisplayName = this.DisplayName,
        Reindex = this.Reindex,
    };
}