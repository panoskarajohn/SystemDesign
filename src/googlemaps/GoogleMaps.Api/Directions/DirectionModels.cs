namespace GoogleMaps.Api.Directions;

public sealed record GenerateDirectionsRequest(
    string UserId,
    string DestinationInput
);

public sealed record GenerateRouteRequest(
    string OriginInput,
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

public sealed record RoutePointResponse(
    double Latitude,
    double Longitude,
    string PlusCode,
    string Label
);

public sealed record RouteSegmentResponse(
    int Order,
    string Instruction,
    string Heading,
    double DistanceMeters
);

public sealed record RouteDrawingResponse(
    string Type,
    string Instruction,
    IReadOnlyList<RoutePointResponse> Coordinates
);

public sealed record GenerateRouteResponse(
    string OriginInput,
    string DestinationInput,
    double TotalDistanceMeters,
    IReadOnlyList<RoutePointResponse> Path,
    IReadOnlyList<RouteSegmentResponse> Segments,
    RouteDrawingResponse Drawing
);
