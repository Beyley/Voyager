using Bunkum.Core.Database;
using Bunkum.RealmDatabase;
using Realms;
using Voyager.Spider;

namespace Voyager.Database;

public class VoyagerDatabaseProvider : RealmDatabaseProvider<VoyagerDatabaseContext>
{
    public override void Warmup()
    {}
    protected override void Migrate(Migration migration, ulong oldVersion)
    {}
    protected override ulong SchemaVersion => 1;
    protected override List<Type> SchemaTypes => new()
    {
        typeof(Index),
        typeof(GopherLine),
    };
    protected override string Filename => "voyager.realm";
}