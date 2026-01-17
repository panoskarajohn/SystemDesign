using MongoDB.Driver;
using Shared.Common;

namespace Proximity.Api.Businesses;

public sealed class BusinessIndexInitializer : IInitializer {
    private const string BusinessCollection = "business";
    private readonly IMongoDatabase _database;

    public BusinessIndexInitializer(IMongoDatabase database) {
        _database = database;
    }

    public async Task InitAsync() {
        var collection = _database.GetCollection<Business>(BusinessCollection);
        var indexKeys = Builders<Business>.IndexKeys.Geo2DSphere(b => b.Location);
        var model = new CreateIndexModel<Business>(indexKeys, new CreateIndexOptions {
            Name = "location_2dsphere_idx"
        });

        await collection.Indexes.CreateOneAsync(model);
    }
}
