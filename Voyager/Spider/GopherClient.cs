using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;
using OneOf;

namespace Voyager.Spider;

public class GopherClient
{
    [Pure]
    public static OneOf<byte[], List<GopherLine>, string> Transaction(string hostname, ushort port, string selector, GopherType type = GopherType.Submenu)
    {
        using TcpClient client = new();
        IAsyncResult result = client.BeginConnect(hostname, port, null, null);
        bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(2));
        if (!success)
            throw new Exception("Failed to connect.");
        client.EndConnect(result);
        
        client.ReceiveTimeout = 1000;
        client.SendTimeout = 1000;

        NetworkStream rawStream = client.GetStream();
        BufferedStream stream = new(rawStream);

        // 0x09 0x0A 0x0D are all invalid characters in selectors
        if (selector.Contains('\x09') || selector.Contains('\x0A') || selector.Contains('\x0D'))
            throw new Exception("Invalid gopher URI?");

        //Write the selector
        stream.Write(Encoding.UTF8.GetBytes(selector));
        stream.WriteByte((byte)'\r');
        stream.WriteByte((byte)'\n');
        stream.Flush();

        MemoryStream memoryStream = new();
        Span<byte> buf = stackalloc byte[4096];
        while (true)
        {
            int read = stream.Read(buf);
            if (read == 0) break;
            memoryStream.Write(buf[..read]);
        }

        switch (type)
        {
            case GopherType.Submenu: {
                List<GopherLine> lines = new();

                memoryStream.Seek(0, SeekOrigin.Begin);
                StreamReader reader = new(memoryStream);
                while (true)
                {
                    string? rawLine = reader.ReadLine();
                    if (rawLine == null) break;
                    //NON COMPLIANT SERVERS, EAT MY ASS
                    if (string.IsNullOrWhiteSpace(rawLine)) continue;
                    //. on its own indicates the end of the submenu
                    if (rawLine == ".") break;

                    string[] splitLine = rawLine.Split("\t");
                    if (splitLine.Length == 0) throw new Exception("Unable to parse gopher submenu!");

                    GopherLine line = new()
                    {
                        Type = (GopherType)(byte)splitLine[0][0],
                        DisplayString = splitLine[0][1..],
                        Hostname = hostname,
                        Port = port,
                        Selector = ""
                    };

                    if (splitLine.Length > 1)
                    {
                        line.Selector = splitLine[1];
                        if (line.Selector.Length > 0 && line.Selector[0] != '/')
                        {
                            line.Selector = "/" + line.Selector;
                        }
                    }
                    if (splitLine.Length > 2) line.Hostname = splitLine[2];
                    if (splitLine.Length > 3)
                        if (ushort.TryParse(splitLine[3], out ushort parsedPort))
                            line.Port = parsedPort;

                    lines.Add(line);
                }

                return lines;
            }
            case GopherType.Error:
            case GopherType.TextFile:
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            case GopherType.CCSONameserver:
            case GopherType.BinHexFile:
            case GopherType.DOSFile:
            case GopherType.UUEncodedFile:
            case GopherType.FullTextSearch:
            case GopherType.Telnet:
            case GopherType.BinaryFile:
            case GopherType.Mirror:
            case GopherType.GIFFile:
            case GopherType.ImageFile:
            case GopherType.Telnet3270:
            case GopherType.BitmapImage:
            case GopherType.MovieFile:
            case GopherType.SoundFile:
            case GopherType.Doc:
            case GopherType.HTML:
            case GopherType.InformationalMessage:
            case GopherType.ImageFileUsuallyPNG:
            case GopherType.DocumentRTFFile:
            case GopherType.SoundFileUsuallyWAV:
            case GopherType.DocumentPDFFile:
            case GopherType.DocumentXMLFile:
            default:
                return memoryStream.ToArray();
        }
    }
}