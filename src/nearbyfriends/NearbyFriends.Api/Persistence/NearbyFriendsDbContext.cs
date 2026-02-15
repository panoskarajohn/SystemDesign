using Microsoft.EntityFrameworkCore;
using Nearby.Api.Users;

namespace Nearby.Api.Persistence;

public sealed class NearbyFriendsDbContext : DbContext {
    public NearbyFriendsDbContext(DbContextOptions<NearbyFriendsDbContext> options) : base(options) {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserFriend> UserFriends => Set<UserFriend>();

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
    }
}
