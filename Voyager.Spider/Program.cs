using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using Realms;
using Voyager;
using Voyager.Database;
using Voyager.Spider;
using Index = Voyager.Database.Index;

using VoyagerDatabaseProvider provider = new();
provider.Initialize();

LoggerConfiguration logConfig = new()
{
    Behaviour = new QueueLoggingBehaviour(),
    MaxLevel = LogLevel.Trace
};

using Logger logger = new(logConfig);

// uncomment this to schedule a re-index of everything :)

// var database = provider.GetContext();
// IQueryable<Index> indexedPages = database.GetAllIndexedPages();
// var count = indexedPages.Count();
// using Transaction transaction = database.Realm.BeginWrite();
// int i = 0;
// foreach (Index index in indexedPages)
// {
//     logger.LogInfo(VoyagerCategory.Spider, "Oh dear {0}/{1}", i, count);
//     database.AddQueuedSelector(new QueuedSelector
//     {
//         _Uri = index.Uri,
//         DisplayName = index.DisplayName,
//         Reindex = true
//     });
//     i++;
// }
// transaction.Commit();
//
// return;

Spider spider = new(new List<Uri>
{
// new("gopher://littlebigrefresh.com")
new("gopher://gopher.floodgap.com")
}, new List<string>
{
    //host git stuff that we need to just blanket block for now
    "parazyd.org",
    "suckless.org",
    "gopher.r-36.net",
    "adamsgaard.dk",
    "bitreich.org",
    //infinite loop (for the love of god beyley fucking implement robots.txt properly at some point)
    "farragofiction.com",
    "yargo.mdns.org",
    //contains a stupid amount of useless links
    "worldofsolitaire.com"
}, provider, logger);
spider.Start();