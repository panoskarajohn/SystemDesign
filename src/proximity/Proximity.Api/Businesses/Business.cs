using MongoDB.Bson.Serialization.Attributes;
using Shared.Mongo;

namespace Proximity.Api.Businesses;

public sealed record Business : IIdentifiable<string> {
    [BsonElement("business_id")]
    public string Id { get; init; } = string.Empty;

    public string Address { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longtitude { get; init; }
}
