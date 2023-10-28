namespace Voyager.Spider;

public enum GopherType : byte
{
    //Canonical types
    TextFile = (byte)'0',
    Submenu = (byte)'1',
    CCSONameserver = (byte)'2',
    Error = (byte)'3',
    BinHexFile = (byte)'4',
    DOSFile = (byte)'5',
    UUEncodedFile = (byte)'6',
    FullTextSearch = (byte)'7',
    Telnet = (byte)'8',
    BinaryFile = (byte)'9',
    Mirror = (byte)'+',
    GIFFile = (byte)'g',
    ImageFile = (byte)'I',
    Telnet3270 = (byte)'T',
    //gopher+ types
    BitmapImage = (byte)':',
    MovieFile = (byte)';',
    SoundFile = (byte)'<',
    //Non-canonical types
    Doc = (byte)'d',
    HTML = (byte)'h',
    InformationalMessage = (byte)'i',
    ImageFileUsuallyPNG = (byte)'p',
    DocumentRTFFile = (byte)'r',
    SoundFileUsuallyWAV = (byte)'s',
    DocumentPDFFile = (byte)'P',
    DocumentXMLFile = (byte)'X'
}