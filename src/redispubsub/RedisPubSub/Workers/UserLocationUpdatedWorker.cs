using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RedisPubSub.Persistence;
using Shared.Redis;
using Shared.Redis.Abstractions;
using Shared.Redis.Contracts;
using StackExchange.Redis;

namespace RedisPubSub.Workers;

public sealed class UserLocationUpdatedWorker : IHostedService {
    // outbox messages will live for 15 minutes
    private static readonly TimeSpan ProcessedEventTtl = TimeSpan.FromMinutes(15);
    private const string UserLocationKeyPrefix = "users:location:";
    private const string ProcessedEventKeyPrefix = "outbox:processed:user-location-updated:";
    private const double NearbyDistanceInKm = 2.0;

    private readonly IRedisChannelSubscriber subscriber;
    private readonly IConnectionMultiplexer multiplexer;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<UserLocationUpdatedWorker> logger;
    private IRedisChannelSubscription? subscription;

    public UserLocationUpdatedWorker(
        IRedisChannelSubscriber subscriber,
        IConnectionMultiplexer multiplexer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<UserLocationUpdatedWorker> logger) {
        this.subscriber = subscriber;
        this.multiplexer = multiplexer;
        this.serviceScopeFactory = serviceScopeFactory;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        subscription = await subscriber.SubscribeAsync<UserLocationUpdatedMessage>(
            RedisChannels.UserLocationUpdated,
            processLocationUpdateAsync,
            cancellationToken);

        logger.LogInformation("Subscribed to channel {Channel}", RedisChannels.UserLocationUpdated);
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        if (subscription is not null) {
            await subscription.DisposeAsync();
        }
    }

    private async Task processLocationUpdateAsync(UserLocationUpdatedMessage message, CancellationToken cancellationToken) {
        try {
            var redisDatabase = multiplexer.GetDatabase();
            var wasMarkedForProcessing = await tryMarkEventForProcessingAsync(redisDatabase, message).ConfigureAwait(false);
            if (!wasMarkedForProcessing) {
                logger.LogInformation(
                    "Skipping already processed location update for user {UserId} at {UpdatedAtUtc}.",
                    message.UserId,
                    message.UpdatedAtUtc);
                return;
            }

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<RedisPubSubDbContext>();
            var subscriberIds = await dbContext.UserLocationSubscriptions
                .Where(x => x.TargetUserId == message.UserId)
                .Select(x => x.SubscriberUserId)
                .ToListAsync(cancellationToken);

            foreach (var subscriberId in subscriberIds) {
                var locationJson = await redisDatabase.StringGetAsync($"{UserLocationKeyPrefix}{subscriberId}").ConfigureAwait(false);
                if (locationJson.IsNullOrEmpty) {
                    continue;
                }

                UserLocationUpdatedMessage? subscriberLocation;
                try {
                    subscriberLocation = JsonSerializer.Deserialize<UserLocationUpdatedMessage>(locationJson.ToString());
                }
                catch (JsonException) {
                    continue;
                }

                if (subscriberLocation is null) {
                    continue;
                }

                var distanceInKm = calculateDistanceInKm(
                    message.Latitude,
                    message.Longitude,
                    subscriberLocation.Latitude,
                    subscriberLocation.Longitude);

                var subscriberNearbyFriendsKey = getNearbyFriendsSetKey(subscriberId);
                var targetUserId = message.UserId.ToString();

                if (distanceInKm <= NearbyDistanceInKm) {
                    await redisDatabase.SetAddAsync(subscriberNearbyFriendsKey, targetUserId).ConfigureAwait(false);
                    continue;
                }

                await redisDatabase.SetRemoveAsync(subscriberNearbyFriendsKey, targetUserId).ConfigureAwait(false);
            }

            logger.LogInformation(
                "Processed location update for user {UserId} with {SubscriberCount} subscription(s).",
                message.UserId,
                subscriberIds.Count);
        }
        catch (Exception exception) {
            logger.LogError(exception, "Failed to process location update for user {UserId}.", message.UserId);
        }
    }

    private static double calculateDistanceInKm(double latitude1, double longitude1, double latitude2, double longitude2) {
        const double earthRadiusInKm = 6371.0;

        var latitudeDistance = toRadians(latitude2 - latitude1);
        var longitudeDistance = toRadians(longitude2 - longitude1);

        var lat1 = toRadians(latitude1);
        var lat2 = toRadians(latitude2);

        var haversine =
            Math.Sin(latitudeDistance / 2) * Math.Sin(latitudeDistance / 2) +
            Math.Cos(lat1) * Math.Cos(lat2) *
            Math.Sin(longitudeDistance / 2) * Math.Sin(longitudeDistance / 2);

        var centralAngle = 2 * Math.Asin(Math.Min(1.0, Math.Sqrt(haversine)));
        return earthRadiusInKm * centralAngle;
    }

    private static double toRadians(double angle) => angle * (Math.PI / 180.0);

    private static string getNearbyFriendsSetKey(Guid subscriberId) => $"nearby:friends:{subscriberId}";

    private static string getProcessedEventKey(UserLocationUpdatedMessage message)
        => $"{ProcessedEventKeyPrefix}{message.UserId:N}:{message.UpdatedAtUtc.ToUniversalTime():O}";

    private static Task<bool> tryMarkEventForProcessingAsync(IDatabase redisDatabase, UserLocationUpdatedMessage message)
        => redisDatabase.StringSetAsync(
            getProcessedEventKey(message),
            "1",
            ProcessedEventTtl,
            when: When.NotExists);
}
