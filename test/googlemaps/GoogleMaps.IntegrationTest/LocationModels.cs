using System.Text.Json.Serialization;

namespace GoogleMaps.IntegrationTest;

public sealed record PostLocationsRequest(
    [property: JsonPropertyName("user_id")] string UserId,
    IReadOnlyList<LocationPointRequest> Locations
);

public sealed record LocationPointRequest(
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);

public sealed record PostLocationsResponse(
    [property: JsonPropertyName("user_id")] string UserId,
    int InsertedCount
);

public sealed record LastUserLocationResponse(
    [property: JsonPropertyName("user_id")] string UserId,
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);
