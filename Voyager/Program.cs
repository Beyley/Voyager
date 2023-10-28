using System.Reflection;
using Bunkum.Protocols.Gemini;
using Bunkum.Protocols.Gopher;
using Bunkum.Protocols.Gopher.Responses.Serialization;
using Bunkum.Serialization.GopherToGemini;
using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using Voyager.Database;

using VoyagerDatabaseProvider provider = new();
provider.Initialize();

LoggerConfiguration logConfig = new()
{
    Behaviour = new QueueLoggingBehaviour(),
    MaxLevel = LogLevel.Trace,
};

using Logger logger = new(logConfig);

BunkumGopherServer gopherServer = new(logConfig);
BunkumGeminiServer geminiServer = new(null, logConfig);

gopherServer.Initialize = s =>
{
    VoyagerDatabaseProvider provider = new();
    
    s.DiscoverEndpointsFromAssembly(Assembly.GetExecutingAssembly());
    s.AddSerializer<BunkumGophermapSerializer>();
    s.UseDatabaseProvider(provider);
};

geminiServer.Initialize = s =>
{
    VoyagerDatabaseProvider provider = new();
    
    s.DiscoverEndpointsFromAssembly(Assembly.GetExecutingAssembly());
    s.AddSerializer<BunkumGophermapGeminiSerializer>();
    s.UseDatabaseProvider(provider);
};

gopherServer.Start();
geminiServer.Start();

#if DEBUG
Console.ReadLine();
#else
await Task.Delay(-1);
#endif
