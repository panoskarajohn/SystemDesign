namespace GoogleMaps.IntegrationTest;

public sealed record PostLocationsRequest(
    string UserId,
    IReadOnlyList<LocationPointRequest> Locations
);

public sealed record LocationPointRequest(
    double Latitude,
    double Longitude,
    DateTimeOffset Timestamp
);

public sealed record PostLocationsResponse(
    string UserId,
    int InsertedCount
);

public sealed record LastUserLocationResponse(
    string UserId,
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
    string UserId,
    string DestinationInput
);

public sealed record GenerateRouteRequest(
    string OriginInput,
    string DestinationInput
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

public sealed record GenerateDirectionsResponse(
    string UserId,
    string DestinationInput,
    string OriginPlusCode,
    string DestinationPlusCode,
    double TotalDistanceMeters,
    IReadOnlyList<DirectionStepResponse> Steps
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
