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
            typeof(QueuedSelector)
        };
    }
    protected override string Filename
    {
        get => "voyager.realm";
    }
    public override void Warmup()
    {}
    protected override void Migrate(Migration migration, ulong oldVersion)
    {}
}