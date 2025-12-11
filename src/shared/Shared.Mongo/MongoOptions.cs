
namespace Shared.Mongo;

public class MongoOptions
{
    public string ConnectionString { get; set; }
    public string DatabaseName { get; set; }
    public bool SeedData { get; set; }
}