namespace Nearby.Api.Users;

public sealed class User {
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<UserFriend> Friends { get; set; } = new List<UserFriend>();
    public ICollection<UserFriend> FriendOf { get; set; } = new List<UserFriend>();
}
