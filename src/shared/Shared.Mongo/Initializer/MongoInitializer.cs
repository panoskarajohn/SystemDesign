using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Shared.Common;
using Shared.Mongo.Seeder;

namespace Shared.Mongo.Initializer;

public interface IMongoInitializer : IInitializer {
}

public class MongoInitializer : IMongoInitializer {
    private readonly IEnumerable<IMongoSeeder> seeders;
    private readonly IMongoDatabase database;
    private readonly SemaphoreSlim semaphore = new SemaphoreSlim(4);
    private readonly MongoOptions options;

    public MongoInitializer(
        IEnumerable<IMongoSeeder> seeders,
        IMongoDatabase database,
        MongoOptions options
    ) {
        this.seeders = seeders;
        this.database = database;
        this.options = options;
    }

    public async Task InitAsync() {
        if (!options.SeedData) {
            return;
        }

        if (seeders == null || !seeders.Any()) {
            return;
        }

        try {
            await semaphore.WaitAsync();
            foreach (var seeder in seeders) {
                await seeder.SeedAsync(database);
            }
        }
        catch (ObjectDisposedException) {
            return;
        }
        finally {
            semaphore.Release();
        }
    }

}