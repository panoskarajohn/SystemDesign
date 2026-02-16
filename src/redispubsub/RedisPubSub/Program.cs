using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Redis;
using Shared.Redis.Abstractions;
using Shared.Redis.Contracts;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddHostedService<ChannelSubscriberWorker>();

var host = builder.Build();
await host.RunAsync();

public sealed class ChannelSubscriberWorker : IHostedService {
    private readonly IRedisChannelSubscriber subscriber;
    private readonly ILogger<ChannelSubscriberWorker> logger;
    private IRedisChannelSubscription? subscription;

    public ChannelSubscriberWorker(IRedisChannelSubscriber subscriber, ILogger<ChannelSubscriberWorker> logger) {
        this.subscriber = subscriber;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        subscription = await subscriber.SubscribeAsync<UserLocationUpdatedMessage>(
            RedisChannels.UserLocationUpdated,
            (message, _) => {
                logger.LogInformation(
                    "User {UserId} location updated to {Latitude},{Longitude} at {UpdatedAtUtc}",
                    message.UserId,
                    message.Latitude,
                    message.Longitude,
                    message.UpdatedAtUtc);

                return Task.CompletedTask;
            },
            cancellationToken);

        logger.LogInformation("Subscribed to channel {Channel}", RedisChannels.UserLocationUpdated);
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        if (subscription is not null) {
            await subscription.DisposeAsync();
        }
    }
}
