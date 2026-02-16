namespace NearbyFriends.IntegrationTest;

public sealed record CreateUserRequest(string Username, string DisplayName);

public sealed record UpdateUserRequest(string Username, string DisplayName);

public sealed record UserResponse(Guid UserId, string Username, string DisplayName, IReadOnlyCollection<Guid> FriendIds);

public sealed record UpdateUserLocationRequest(double Latitude, double Longitude);

public sealed record UpdateUserLocationResponse(
    Guid UserId,
    double Latitude,
    double Longitude,
    DateTime UpdatedAtUtc,
    long PublishedToSubscribers);

public sealed record UserLocationResponse(
    Guid UserId,
    double Latitude,
    double Longitude,
    DateTime UpdatedAtUtc);
