using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Redis;
using Shared.Redis.Abstractions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddRedis(builder.Configuration);
builder.Services.AddHostedService<ChannelSubscriberWorker>();
builder.Services.AddHostedService<ChannelPublisherWorker>();

var host = builder.Build();
await host.RunAsync();

public sealed record DemoMessage(string Source, string Text, DateTime CreatedAtUtc);

public static class DemoChannels {
    public const string Notifications = "demo.notifications";
}

public sealed class ChannelSubscriberWorker : IHostedService {
    private readonly IRedisChannelSubscriber subscriber;
    private readonly ILogger<ChannelSubscriberWorker> logger;
    private IRedisChannelSubscription? subscription;

    public ChannelSubscriberWorker(IRedisChannelSubscriber subscriber, ILogger<ChannelSubscriberWorker> logger) {
        this.subscriber = subscriber;
        this.logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        subscription = await subscriber.SubscribeAsync<DemoMessage>(
            DemoChannels.Notifications,
            (message, _) => {
                logger.LogInformation(
                    "Received message from {Source} at {CreatedAtUtc}: {Text}",
                    message.Source,
                    message.CreatedAtUtc,
                    message.Text);

                return Task.CompletedTask;
            },
            cancellationToken);

        logger.LogInformation("Subscribed to channel {Channel}", DemoChannels.Notifications);
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        if (subscription is not null) {
            await subscription.DisposeAsync();
        }
    }
}

public sealed class ChannelPublisherWorker : BackgroundService {
    private readonly IRedisChannelPublisher publisher;
    private readonly ILogger<ChannelPublisherWorker> logger;

    public ChannelPublisherWorker(IRedisChannelPublisher publisher, ILogger<ChannelPublisherWorker> logger) {
        this.publisher = publisher;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            var payload = new DemoMessage("publisher", "Hello from Redis pub/sub", DateTime.UtcNow);
            var deliveredToSubscribers = await publisher.PublishAsync(DemoChannels.Notifications, payload, stoppingToken);

            logger.LogInformation(
                "Published message to {Channel} (subscribers: {SubscriberCount})",
                DemoChannels.Notifications,
                deliveredToSubscribers);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
