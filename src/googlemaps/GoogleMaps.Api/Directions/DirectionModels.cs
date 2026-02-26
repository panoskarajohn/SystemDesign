using System.Text.Json.Serialization;

namespace GoogleMaps.Api.Directions;

public sealed record GenerateDirectionsRequest(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("destination_input")] string DestinationInput
);

public sealed record GenerateDirectionsResponse(
    [property: JsonPropertyName("user_id")] string UserId,
    [property: JsonPropertyName("destination_input")] string DestinationInput,
    [property: JsonPropertyName("origin_plus_code")] string OriginPlusCode,
    [property: JsonPropertyName("destination_plus_code")] string DestinationPlusCode,
    [property: JsonPropertyName("total_distance_meters")] double TotalDistanceMeters,
    IReadOnlyList<DirectionStepResponse> Steps
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
