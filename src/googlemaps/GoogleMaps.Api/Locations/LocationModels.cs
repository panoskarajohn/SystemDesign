namespace GoogleMaps.Api.Locations;

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
