namespace GoogleMaps.Api.Directions;

public sealed record GenerateDirectionsRequest(
    string UserId,
    string DestinationInput
);

public sealed record GenerateDirectionsResponse(
    string UserId,
    string DestinationInput,
    string OriginPlusCode,
    string DestinationPlusCode,
    double TotalDistanceMeters,
    IReadOnlyList<DirectionStepResponse> Steps
);

public sealed record DirectionStepResponse(
    int Order,
    string Instruction,
    string Heading,
    double DistanceMeters,
    string TargetInput,
    string FromPlusCode,
    string ToPlusCode
);
