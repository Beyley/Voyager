using Bunkum.RealmDatabase;
using Realms;
using Voyager.Spider;

namespace Voyager.Database;

public class VoyagerDatabaseProvider : RealmDatabaseProvider<VoyagerDatabaseContext>
{
    protected override ulong SchemaVersion
    {
        get => 1;
    }
    protected override List<Type> SchemaTypes
    {
        get => new()
        {
            typeof(Index),
            typeof(GopherLine),
            typeof(QueuedSelector),
            typeof(CrawledHost)
        };
    }
    protected override string Filename
    {
        get => "voyager.realm";
    }
    public override void Warmup()
    {
        VoyagerDatabaseContext database = this.GetContext();
        _ = database.GetAllIndexedPages().Count();
    }
    protected override void Migrate(Migration migration, ulong oldVersion)
    {}
}