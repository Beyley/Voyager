using Voyager.Database;
using Voyager.Spider;

using VoyagerDatabaseProvider provider = new();
provider.Initialize();

Spider spider = new(new List<Uri>
{
    new("gopher://littlebigrefresh.com")
    // new("gopher://gopher.floodgap.com")
}, provider);
spider.Start();

// LoggerConfiguration logConfig = new LoggerConfiguration
// {
//     Behaviour = new QueueLoggingBehaviour(),
//     MaxLevel = LogLevel.Trace,
// };
//
//
// BunkumGopherServer gopherServer = new(logConfig);
// BunkumGeminiServer geminiServer = new(null, logConfig);
//
// gopherServer.Initialize = s =>
// {
//     s.DiscoverEndpointsFromAssembly(Assembly.GetExecutingAssembly());
//     s.AddSerializer<BunkumGophermapSerializer>();
// };
//
// geminiServer.Initialize = s =>
// {
//     s.DiscoverEndpointsFromAssembly(Assembly.GetExecutingAssembly());
//     s.AddSerializer<BunkumGophermapGeminiSerializer>();
// };
//
// gopherServer.Start();
// geminiServer.Start();
//
// await Task.Delay(-1);