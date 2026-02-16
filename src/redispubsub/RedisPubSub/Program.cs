using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisPubSub.Persistence;
using Shared.Postgres;
using Shared.Redis;
using Shared.Redis.Abstractions;
using Shared.Redis.Contracts;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddPostgres<RedisPubSubDbContext>(builder.Configuration);
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddHostedService<ChannelSubscriberWorker>();

var host = builder.Build();
await host.RunAsync();

public sealed class ChannelSubscriberWorker : IHostedService {
    private const string NearbyFriendsSetKey = "nearby:friends";
    private const string UserLocationKeyPrefix = "users:location:";
    private const double NearbyDistanceInKm = 2.0;

    private readonly IRedisChannelSubscriber subscriber;
    private readonly IConnectionMultiplexer multiplexer;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly ILogger<ChannelSubscriberWorker> logger;
    private IRedisChannelSubscription? subscription;

    public ChannelSubscriberWorker(
        IRedisChannelSubscriber subscriber,
        IConnectionMultiplexer multiplexer,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<ChannelSubscriberWorker> logger) {
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

    private async Task processLocationUpdateAsync(UserLocationUpdatedMessage message, CancellationToken cancellationToken) {
        try {
            var redisDatabase = multiplexer.GetDatabase();

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
                    subscriberLocation = System.Text.Json.JsonSerializer.Deserialize<UserLocationUpdatedMessage>(locationJson!.ToString());
                }
                catch (System.Text.Json.JsonException) {
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

                if (distanceInKm > NearbyDistanceInKm) {
                    continue;
                }

                var nearbyMember = $"{subscriberId}:{message.UserId}";
                await redisDatabase.SetAddAsync(NearbyFriendsSetKey, nearbyMember).ConfigureAwait(false);
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

    public async Task StopAsync(CancellationToken cancellationToken) {
        if (subscription is not null) {
            await subscription.DisposeAsync();
        }
    }
}
