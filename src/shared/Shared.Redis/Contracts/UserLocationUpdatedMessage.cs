namespace Shared.Redis.Contracts;

public sealed record UserLocationUpdatedMessage(
    Guid UserId,
    double Latitude,
    double Longitude,
    DateTime UpdatedAtUtc);
