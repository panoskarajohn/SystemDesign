using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using Shared.Mongo;

namespace Geocoding.Api.Mongo;

public sealed record GeoLocationDocument : IIdentifiable<string> {
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string Input { get; init; } = string.Empty;
    public string Language { get; init; } = string.Empty;

    [BsonElement("region_bias")]
    public string RegionBias { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;

    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; }
}
