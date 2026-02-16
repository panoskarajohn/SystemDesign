namespace NearbyFriends.Api.Users;

public sealed class UserLocationSubscription {
    public Guid SubscriberUserId { get; set; }
    public Guid TargetUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
