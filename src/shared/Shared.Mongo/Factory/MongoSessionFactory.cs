using MongoDB.Driver;

namespace Shared.Mongo.Factory;

public interface IMongoSessionFactory {
    Task<IClientSessionHandle> CreateAsync();
}

public class MongoSessionFactory : IMongoSessionFactory {
    private readonly IMongoClient _mongoClient;

    public MongoSessionFactory(IMongoClient mongoClient) {
        _mongoClient = mongoClient;
    }

    public async Task<IClientSessionHandle> CreateAsync() {
        return await _mongoClient.StartSessionAsync();
    }
}