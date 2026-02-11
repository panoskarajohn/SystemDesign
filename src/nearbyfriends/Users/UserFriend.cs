namespace Nearby.Api.Users;

public sealed class UserFriend {
    public Guid UserId { get; set; }
    public Guid FriendId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public User User { get; set; } = default!;
    public User Friend { get; set; } = default!;
}
