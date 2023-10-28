using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using Voyager.Database;
using Voyager.Spider;

using VoyagerDatabaseProvider provider = new();
provider.Initialize();

LoggerConfiguration logConfig = new()
{
    Behaviour = new QueueLoggingBehaviour(),
    MaxLevel = LogLevel.Trace,
};

using Logger logger = new(logConfig);

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
    //contains a stupid amount of useless links
    "worldofsolitaire.com",
}, provider, logger);
spider.Start();