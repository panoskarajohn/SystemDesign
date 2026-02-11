namespace NearbyFriends.IntegrationTest;

public sealed record CreateUserRequest(string Username, string DisplayName);

public sealed record UpdateUserRequest(string Username, string DisplayName);

public sealed record UserResponse(Guid UserId, string Username, string DisplayName, IReadOnlyCollection<Guid> FriendIds);
