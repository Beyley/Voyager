using System.Net;
using System.Text;
using Bunkum.Core;
using Bunkum.Core.Configuration;
using Bunkum.Core.Endpoints;
using Bunkum.Core.Responses;
using Bunkum.Protocols.Gemini;
using Bunkum.Protocols.Gemini.Responses;
using Bunkum.Protocols.Gopher;
using Bunkum.Protocols.Gopher.Responses;
using Bunkum.Protocols.Gopher.Responses.Items;
using Realms;
using Voyager.Database;
using Voyager.Extensions;
using Index = Voyager.Database.Index;

namespace Voyager.Endpoints;

public class RootEndpoints : EndpointGroup
{
    [GopherEndpoint("/")]
    [GeminiEndpoint("/")]
    public List<GophermapItem> Root(RequestContext context, VoyagerDatabaseContext database, BunkumConfig config)
    {
        List<GophermapItem> map = new();
        map.AddHeading(context, "Voyager", 1);
        map.AddHeading(context, "A gopher/gemini search engine", 2);

        IQueryable<Index> indexedPages = database.GetAllIndexedPages();
        IQueryable<CrawledHost> crawledHosts = database.GetAllCrawledHosts();
        IQueryable<QueuedSelector> queuedSelectors = database.GetAllQueuedSelectors();

        map.Add(new GophermapMessage(""));
        map.Add(new GophermapMessage($"{indexedPages.Count():N0} selectors indexed ({indexedPages.Count(i => i.HasBlankLine || i.LineMissingType || i.MissingDirectoryTerminator)} breaking spec)"));
        map.Add(new GophermapMessage($"{crawledHosts.Count():N0} total hosts crawled ({crawledHosts.Count(h => h.Failed)} hosts are inaccessible)"));
        map.Add(new GophermapMessage($"{queuedSelectors.Count():N0} selectors queued to be crawled"));
        if (context.IsGemini())
            map.Add(new GophermapMessage("=> /search Search"));
        else
            map.Add(new GophermapLink(GophermapItemType.IndexSearchServer, "Search", config, "/search"));

        return map;
    }
    
    [GopherEndpoint("/stats")]
    [GeminiEndpoint("/stats")]
    public List<GophermapItem> Stats(RequestContext context, VoyagerDatabaseContext database, BunkumConfig config)
    {
        List<GophermapItem> map = new();
        map.AddHeading(context, "Voyager Stats", 1);
        map.AddHeading(context, "All the infos", 2);

        IQueryable<Index> indexedPages = database.GetAllIndexedPages();
        IQueryable<CrawledHost> crawledHosts = database.GetAllCrawledHosts();
        IQueryable<QueuedSelector> queuedSelectors = database.GetAllQueuedSelectors();

        int hostCount = crawledHosts.Count();
        int failedHostCount = crawledHosts.Count(h => h.Failed);
        
        map.Add(new GophermapMessage(""));
        map.AddHeading(context, "General info", 3);
        map.Add(new GophermapMessage($"{indexedPages.Count():N0} selectors indexed"));
        map.Add(new GophermapMessage($"{crawledHosts.Count():N0} total hosts crawled. {crawledHosts.Count(h => h.Failed)} hosts are inaccessible ({failedHostCount/(float)hostCount * 100:N0}%)"));
        map.Add(new GophermapMessage($"{queuedSelectors.Count():N0} selectors queued to be crawled"));

        int missingDirTerminator = indexedPages.Count(i => i.MissingDirectoryTerminator);
        int hasBlankLine = indexedPages.Count(i => i.HasBlankLine);
        int lineMissingType = indexedPages.Count(i => i.LineMissingType);
        int specNoncompliant = indexedPages.Count(i => i.LineMissingType || i.HasBlankLine || i.MissingDirectoryTerminator);
        
        map.AddHeading(context, "How spec compliant is gopherspace?", 3);
        map.Add(new GophermapMessage($"{missingDirTerminator:N0} selectors are missing the directory terminator"));
        map.Add(new GophermapMessage($"{hasBlankLine:N0} selectors contain invalid blank lines"));
        map.Add(new GophermapMessage($"{lineMissingType:N0} selectors have lines which are missing a type"));
        map.Add(new GophermapMessage($"Overall, {specNoncompliant:N0} selectors violate the spec ({(float)specNoncompliant * 100.0f / indexedPages.Count():N0}%)"));

        map.AddHeading(context, "Encoding usages", 3);

        string[] encodings = {"us-ascii", "utf-8", "windows-1252", "ibm855", "koi8-r", "windows-1251", "gb18030", "x-mac-cyrillic", "windows-1255", "big5"};
        int accountedFor = 0;
        foreach (string encoding in encodings)
        {
            int count = indexedPages.Count(i => i.Encoding == encoding);
            map.Add(new GophermapMessage($"{encoding}: {count:N0}"));
            accountedFor += count;
        }
        int unaccountedFor = indexedPages.Count() - accountedFor;
        if(unaccountedFor > 0)
            map.Add(new GophermapMessage($"unknown (update list in code): {unaccountedFor}"));
        
        return map;
    }

    [GeminiEndpoint("/search", GeminiContentTypes.Gemtext)]
    public Response SearchGemini(RequestContext context, VoyagerDatabaseContext database)
    {
        string? query = context.QueryString.Get("input");
        if (query == null)
            return new Response(HttpStatusCode.Continue);
        IQueryable<Index> found = database.Search(query);

        StringBuilder gemtext = new();

        gemtext.AppendLine("# Voyager");
        gemtext.AppendLine($"### Found {found.Count():N0} results");

        foreach (Index index in found)
        {
            Uri uri = new(index.Uri);
            uri = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}/1{uri.AbsolutePath.Replace(" ", "%20")}");

            gemtext.AppendLine($"=> {uri} {index.DisplayName}");
        }

        return new Response(gemtext.ToString(), GeminiContentTypes.Gemtext);
    }

    [GopherEndpoint("/search")]
    public List<GophermapItem> SearchGopher(RequestContext context, VoyagerDatabaseContext database)
    {
        string? query = context.QueryString.Get("input");
        if (query == null)
            return new List<GophermapItem>
            {
                new()
                {
                    ItemType = GophermapItemType.Error,
                    DisplayText = "Error....",
                    Hostname = "error.host"
                }
            };
        IQueryable<Index> found = database.Search(query);

        List<GophermapItem> map = new();

        map.Add(new GophermapMessage("Voyager"));
        map.Add(new GophermapMessage($"Found {found.Count():N0} results"));

        foreach (Index index in found)
        {
            Uri uri = new(index.Uri);
            uri = new Uri($"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath.Replace(" ", "%20")}");

            map.Add(new GophermapLink(GophermapItemType.Directory, index.DisplayName ?? uri.ToString(), uri));
        }

        return map;
    }
}