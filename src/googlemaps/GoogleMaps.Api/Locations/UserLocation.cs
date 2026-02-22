using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver.GeoJsonObjectModel;
using Shared.Mongo;

namespace GoogleMaps.Api.Locations;

public sealed record UserLocation : IIdentifiable<string> {
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    [BsonElement("user_id")]
    public string UserId { get; init; } = string.Empty;

    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; init; } = null!;
    public DateTimeOffset Timestamp { get; init; }

    [BsonElement("batch_window_seconds")]
    public int BatchWindowSeconds { get; init; } = 15;
}
