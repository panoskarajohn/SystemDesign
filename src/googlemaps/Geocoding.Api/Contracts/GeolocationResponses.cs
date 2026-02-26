namespace Geocoding.Api.Contracts;

public sealed record InsertGeolocationResponse(string Id);

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

public sealed record GeolocationPointResponse(double Longitude, double Latitude);
