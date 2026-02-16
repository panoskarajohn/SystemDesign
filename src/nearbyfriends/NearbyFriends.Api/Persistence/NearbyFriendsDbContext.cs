using Microsoft.EntityFrameworkCore;
using NearbyFriends.Api.Users;

namespace NearbyFriends.Api.Persistence;

public sealed class NearbyFriendsDbContext : DbContext {
    public NearbyFriendsDbContext(DbContextOptions<NearbyFriendsDbContext> options) : base(options) {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserFriend> UserFriends => Set<UserFriend>();
    public DbSet<UserLocationSubscription> UserLocationSubscriptions => Set<UserLocationSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<User>(entity => {
            entity.ToTable("users");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).HasColumnName("id");
            entity.Property(x => x.Username).HasColumnName("username").HasMaxLength(50).IsRequired();
            entity.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(120).IsRequired();
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").IsRequired();
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<UserFriend>(entity => {
            entity.ToTable("user_friends");
            entity.HasKey(x => new { x.UserId, x.FriendId });
            entity.Property(x => x.UserId).HasColumnName("user_id");
            entity.Property(x => x.FriendId).HasColumnName("friend_id");
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.HasOne(x => x.User)
                .WithMany(x => x.Friends)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Friend)
                .WithMany(x => x.FriendOf)
                .HasForeignKey(x => x.FriendId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(x => x.HasCheckConstraint("ck_user_friends_distinct", "user_id <> friend_id"));
        });

        modelBuilder.Entity<UserLocationSubscription>(entity => {
            entity.ToTable("user_location_subscriptions");
            entity.HasKey(x => new { x.SubscriberUserId, x.TargetUserId });
            entity.Property(x => x.SubscriberUserId).HasColumnName("subscriber_user_id");
            entity.Property(x => x.TargetUserId).HasColumnName("target_user_id");
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.SubscriberUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(x => x.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.ToTable(x => x.HasCheckConstraint("ck_user_location_subscriptions_distinct", "subscriber_user_id <> target_user_id"));
        });
    }
}
