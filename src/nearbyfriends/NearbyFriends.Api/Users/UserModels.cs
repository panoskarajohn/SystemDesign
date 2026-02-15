namespace NearbyFriends.Api.Users;

public record CreateUserRequest(string Username, string DisplayName);

public record UpdateUserRequest(string Username, string DisplayName);

public record UserResponse(Guid UserId, string Username, string DisplayName, IReadOnlyCollection<Guid> FriendIds);
