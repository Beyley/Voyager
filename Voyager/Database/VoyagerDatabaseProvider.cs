using System.Text;
using Bunkum.RealmDatabase;
using Realms;
using Voyager.Gopher;

namespace Voyager.Database;

public class VoyagerDatabaseProvider : RealmDatabaseProvider<VoyagerDatabaseContext>
{
    protected override ulong SchemaVersion => 5;
    protected override List<Type> SchemaTypes => new()
    {
        typeof(Index),
        typeof(GopherLine),
        typeof(QueuedSelector),
        typeof(CrawledHost),
    };

    protected override string Filename => "voyager.realm";
    public override void Warmup()
    {
        VoyagerDatabaseContext database = this.GetContext();
        _ = database.GetAllIndexedPages().Count();
    }
    protected override void Migrate(Migration migration, ulong oldVersion)
    {
        // List<Index> oldIndexes = migration.OldRealm.All<Index>().ToList();
        List<Index> newIndexes = migration.NewRealm.All<Index>().ToList();
        for (int i = 0; i < newIndexes.Count; i++)
        {
            // Index oldIndex = oldIndexes[i];
            Index newIndex = newIndexes[i];

            if (oldVersion < 4)
            {
                newIndex.Encoding = Encoding.UTF8.WebName;
            }

            if (oldVersion < 5)
            {
                newIndex.ResponseTime = -1;
            }
        }

        List<QueuedSelector> newQueuedSelectors = migration.NewRealm.All<QueuedSelector>().ToList();
        foreach (QueuedSelector queuedSelector in newQueuedSelectors)
        {
            if (oldVersion < 2)
            {
                queuedSelector.Reindex = false;
            }
        }
    }
}