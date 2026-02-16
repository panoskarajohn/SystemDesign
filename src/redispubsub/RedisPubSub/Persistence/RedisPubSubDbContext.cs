using Microsoft.EntityFrameworkCore;

namespace RedisPubSub.Persistence;

public sealed class RedisPubSubDbContext : DbContext {
    public RedisPubSubDbContext(DbContextOptions<RedisPubSubDbContext> options) : base(options) {
    }

    public DbSet<UserLocationSubscription> UserLocationSubscriptions => Set<UserLocationSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.Entity<UserLocationSubscription>(entity => {
            entity.ToTable("user_location_subscriptions");
            entity.HasKey(x => new { x.SubscriberUserId, x.TargetUserId });
            entity.Property(x => x.SubscriberUserId).HasColumnName("subscriber_user_id");
            entity.Property(x => x.TargetUserId).HasColumnName("target_user_id");
            entity.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").IsRequired();
        });
    }
}
