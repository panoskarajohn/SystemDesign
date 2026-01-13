using System.Text.Json.Serialization;

public sealed record CreateBusinessRequest(
    [property: JsonPropertyName("business_id")] string BusinessId,
    string Address,
    string City,
    string State,
    string Country,
    double Latitude,
    double Longtitude
);

public sealed record UpdateBusinessRequest(
    string Address,
    string City,
    string State,
    string Country,
    double Latitude,
    double Longtitude
);

public sealed record BusinessResponse(
    [property: JsonPropertyName("business_id")] string BusinessId,
    string Address,
    string City,
    string State,
    string Country,
    double Latitude,
    double Longtitude
);
