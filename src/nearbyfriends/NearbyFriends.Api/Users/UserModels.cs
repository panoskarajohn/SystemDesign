namespace NearbyFriends.Api.Users;

public record CreateUserRequest(string Username, string DisplayName);

public record UpdateUserRequest(string Username, string DisplayName);

public record UserResponse(Guid UserId, string Username, string DisplayName, IReadOnlyCollection<Guid> FriendIds);

public record UpdateUserLocationRequest(double Latitude, double Longitude);

public record UpdateUserLocationResponse(
    Guid UserId,
    double Latitude,
    double Longitude,
    DateTime UpdatedAtUtc,
    long PublishedToSubscribers);
