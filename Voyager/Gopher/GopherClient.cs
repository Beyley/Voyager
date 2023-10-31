using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net.Sockets;
using System.Text;
using Ude;
using Index = Voyager.Database.Index;

namespace Voyager.Gopher;

public static class GopherClient
{
    [Pure]
    public static Index Transaction(string hostname, ushort port, string selector, GopherType type = GopherType.Submenu)
    {
        Index response = new();

        long start = Stopwatch.GetTimestamp();

        using TcpClient client = new();
        IAsyncResult result = client.BeginConnect(hostname, port, null, null);
        bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));
        if (!success)
            throw new TimeoutException($"Failed to connect to {hostname}.");
        client.EndConnect(result);

        client.ReceiveTimeout = 3000;
        client.SendTimeout = 3000;

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

        MemoryStream memoryStream = new(4096);
        Span<byte> buf = stackalloc byte[4096];
        while (true)
        {
            int read = stream.Read(buf);
            if (read == 0) break;
            memoryStream.Write(buf[..read]);
        }
        byte[] data = memoryStream.ToArray();
        Span<byte> dataSpan = data.AsSpan();
        memoryStream.SetLength(0);
        memoryStream.Dispose();

        CharsetDetector charsetDetector = new();

        switch (type)
        {
            case GopherType.Submenu: {
                List<GopherLine> lines = new();

                Encoding encoding = Encoding.UTF8;
                charsetDetector.Reset();
                charsetDetector.Feed(data, 0, data.Length);
                charsetDetector.DataEnd();
                if (charsetDetector.Charset != null) encoding = charsetDetector.Charset;

                // memoryStream.Seek(0, SeekOrigin.Begin);
                // StreamReader reader = new(memoryStream);
                bool missingDirectoryEnd = true;
                int lastFound = 0;
                while (true)
                {
                    if (data.Length == 0)
                    {
                        response.HasBlankLine = true;
                        break;
                    }
                    int idx = Array.IndexOf(data, (byte)'\n', lastFound);
                    
                    //If theres no \n, break out
                    if (idx == -1) break;

                    //hack to escape weird situation
                    if (idx == lastFound)
                    {
                        lastFound++;
                        continue;
                    }
                    
                    //Get the raw line (excluding the \r)
                    Span<byte> rawLine = dataSpan[lastFound .. (idx - 1)];
                    
                    //Update the last found \n location
                    lastFound = idx + 1;
                    
                    //NON COMPLIANT SERVERS, EAT MY ASS
                    if (rawLine.Length == 0)
                    {
                        response.HasBlankLine = true;
                        continue;
                    }

                    //. on its own indicates the end of the submenu
                    if (rawLine[0] == '.' && rawLine.Length == 1)
                    {
                        missingDirectoryEnd = false;
                        break;
                    }

                    //Get the real line in string form
                    string line = encoding.GetString(rawLine);
                    //Split it by tabs
                    string[] splitLine = line.Split("\t");
                    //Ignore blank lines...
                    if (splitLine.Length == 0)
                    {
                        response.HasBlankLine = true;
                        continue;
                    }

                    //NON COMPLIANT SERVERS WHYYYYY
                    if (string.IsNullOrWhiteSpace(splitLine[0]))
                    {
                        response.LineMissingType = true;
                        continue;
                    }

                    GopherLine gopherLine = new()
                    {
                        Type = (GopherType)(byte)splitLine[0][0],
                        DisplayString = splitLine[0][1..].Trim(),
                        Hostname = hostname.Trim(),
                        Port = port,
                        Selector = ""
                    };

                    if (splitLine.Length > 1)
                    {
                        gopherLine.Selector = splitLine[1].Trim();
                        if (gopherLine.Selector.Length > 0 && gopherLine.Selector[0] != '/')
                        {
                            gopherLine.Selector = "/" + gopherLine.Selector;
                        }
                    }
                    if (splitLine.Length > 2) gopherLine.Hostname = splitLine[2].Trim();
                    if (splitLine.Length > 3)
                        if (ushort.TryParse(splitLine[3].Trim(), out ushort parsedPort))
                            gopherLine.Port = parsedPort;

                    lines.Add(gopherLine);
                }

                response.Encoding = encoding.WebName;
                response.MissingDirectoryTerminator = missingDirectoryEnd;
                response.Data = lines;
                break;
            }
            case GopherType.Error:
            case GopherType.TextFile:
                charsetDetector.Reset();
                charsetDetector.Feed(data, 0, data.Length);
                charsetDetector.DataEnd();

                response.Data = charsetDetector.Charset.GetString(data);
                break;
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
                response.Data = memoryStream.ToArray();
                break;
        }

        long time = (Stopwatch.GetTimestamp() - start) * 1000;
        response.ResponseTime = (float)time / Stopwatch.Frequency;

        return response;
    }
}