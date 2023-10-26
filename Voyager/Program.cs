using System.Reflection;
using Bunkum.Protocols.Gemini;
using Bunkum.Protocols.Gopher;
using Bunkum.Protocols.Gopher.Responses.Serialization;
using Bunkum.Serialization.GopherToGemini;
using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using Voyager.Database;
using Voyager.Spider;

using VoyagerDatabaseProvider provider = new();
provider.Initialize();

Spider spider = new(new List<Uri>
{
    new Uri("gopher://gopher.floodgap.com")
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