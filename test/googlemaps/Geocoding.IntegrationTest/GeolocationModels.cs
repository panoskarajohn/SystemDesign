using System.Text.Json.Serialization;

namespace Geocoding.IntegrationTest;

public sealed record GeolocationPointRequest(double Longitude, double Latitude);

public sealed record InsertGeolocationRequest(
    GeolocationPointRequest Location,
    string Input,
    string Language,
    string RegionBias,
    string Source
);

public sealed record InsertGeolocationResponse(string Id);

public sealed record GeolocationResponse(
    string Id,
    string Input,
    string Language,
    string RegionBias,
    string Source,
    [property: JsonPropertyName("plusCode")] string PlusCode,
    GeolocationPointResponse Location,
    DateTimeOffset Timestamp
);

public sealed record GeolocationPointResponse(double Longitude, double Latitude);
