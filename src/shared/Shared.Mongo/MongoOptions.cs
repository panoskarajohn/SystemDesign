
using System.Text.Json.Serialization;

namespace Shared.Mongo;

public class MongoOptions {
    [JsonPropertyName("connectionString")]
    public required string ConnectionString { get; set; }
    [JsonPropertyName("database")]
    public required string Database { get; set; }
    [JsonPropertyName("seed")]
    public bool SeedData { get; set; }
}