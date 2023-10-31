using Realms;
using Voyager.Gopher;
using OneOf;

namespace Voyager.Database;

public partial class Index : IRealmObject
{
    [Ignored]
    public OneOf<byte[], List<GopherLine>, string> Data { get; set; }
    
    [PrimaryKey]
    public string Uri { get; set; }
    public IList<GopherLine> Items { get; }
    public DateTimeOffset Time { get; set; }
    [Indexed(IndexType.FullText)]
    public string? DisplayName { get; set; }
    /// <summary>
    ///     The response time, in milliseconds
    /// </summary>
    public float ResponseTime { get; set; }
    /// <summary>
    ///     Set when the server fails to send the directory terminator (. on a blank line)
    /// </summary>
    public bool MissingDirectoryTerminator { get; set; }
    /// <summary>
    ///     Set when the server sends a completely blank line
    /// </summary>
    public bool HasBlankLine { get; set; }
    /// <summary>
    ///     Set when a line is missing type + display name
    /// </summary>
    public bool LineMissingType { get; set; }
    /// <summary>
    ///     The encoding of the returned data
    /// </summary>
    public string Encoding { get; set; }
}