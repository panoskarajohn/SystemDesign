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

public sealed record GeolocationPointRequest(double Longitude, double Latitude);

public sealed record InsertGeolocationRequest(
    GeolocationPointRequest Location,
    string Input,
    string Language,
    string RegionBias,
    string Source
);

public sealed record InsertGeolocationResponse(string Id);

public sealed record GeolocationPointResponse(double Longitude, double Latitude);

public sealed record GeolocationResponse(
    string Id,
    string Input,
    string Language,
    string RegionBias,
    string Source,
    string PlusCode,
    GeolocationPointResponse Location,
    DateTimeOffset Timestamp
);

public sealed record GenerateDirectionsRequest(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("destination_input")] string DestinationInput
);

public sealed record DirectionStepResponse(
    int Order,
    string Instruction,
    string Heading,
    [property: JsonPropertyName("distance_meters")] double DistanceMeters,
    [property: JsonPropertyName("target_input")] string TargetInput,
    [property: JsonPropertyName("from_plus_code")] string FromPlusCode,
    [property: JsonPropertyName("to_plus_code")] string ToPlusCode
);

public sealed record GenerateDirectionsResponse(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("destination_input")] string DestinationInput,
    [property: JsonPropertyName("origin_plus_code")] string OriginPlusCode,
    [property: JsonPropertyName("destination_plus_code")] string DestinationPlusCode,
    [property: JsonPropertyName("total_distance_meters")] double TotalDistanceMeters,
    IReadOnlyList<DirectionStepResponse> Steps
);
