using Realms;

namespace Voyager.Spider;

public partial class GopherLine : IEmbeddedObject
{
    private byte _Type { get; set; }
    [Ignored]
    public GopherType Type { get => (GopherType)this._Type; set => this._Type = (byte)value; }
    public string DisplayString { get; set; }
    public string Selector { get; set; }
    public string Hostname { get; set; }
    public int Port { get; set; }
}