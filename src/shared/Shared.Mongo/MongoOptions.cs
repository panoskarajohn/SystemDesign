
namespace Shared.Mongo;

public class MongoOptions {
    public required string ConnectionString { get; set; }
    public required string DatabaseName { get; set; }
    public bool SeedData { get; set; }
}