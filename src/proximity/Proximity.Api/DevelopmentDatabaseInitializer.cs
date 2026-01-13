using Microsoft.Extensions.Hosting;
using MongoDB.Driver;
using Shared.Common;


public sealed class DevelopmentDatabaseInitializer : IInitializer {
    private const string BusinessCollection = "business";
    private readonly IMongoDatabase _database;
    private readonly IHostEnvironment _environment;

    public DevelopmentDatabaseInitializer(IMongoDatabase database, IHostEnvironment environment) {
        _database = database;
        _environment = environment;
    }

    public async Task InitAsync() {
        if (!_environment.IsDevelopment()) {
            return;
        }

        var names = await _database.ListCollectionNames().ToListAsync();
        if (!names.Contains(BusinessCollection)) {
            return;
        }

        await _database.DropCollectionAsync(BusinessCollection);
    }
}
