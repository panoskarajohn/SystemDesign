# Shared.Redis Pub/Sub

`Shared.Redis` provides a typed abstraction on top of Redis channels.

## Registration

```csharp
using Shared.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRedis(builder.Configuration);
```

Configuration:

```json
{
  "redis": {
    "connectionString": "localhost:6379"
  }
}
```

## Publish Example

```csharp
using Shared.Redis.Abstractions;

public sealed class NotificationPublisher {
    private readonly IRedisChannelPublisher publisher;

    public NotificationPublisher(IRedisChannelPublisher publisher) {
        this.publisher = publisher;
    }

    public Task SendAsync(Guid userId, string text, CancellationToken cancellationToken) {
        var payload = new UserNotification(userId, text, DateTime.UtcNow);
        return publisher.PublishAsync("notifications.user", payload, cancellationToken);
    }
}

public sealed record UserNotification(Guid UserId, string Message, DateTime SentAtUtc);
```

## Subscribe Example

```csharp
using Shared.Redis.Abstractions;

public sealed class NotificationSubscriber : IHostedService {
    private readonly IRedisChannelSubscriber subscriber;
    private IRedisChannelSubscription? subscription;

    public NotificationSubscriber(IRedisChannelSubscriber subscriber) {
        this.subscriber = subscriber;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        subscription = await subscriber.SubscribeAsync<UserNotification>(
            "notifications.user",
            (message, ct) => {
                Console.WriteLine($"Received for {message.UserId}: {message.Message}");
                return Task.CompletedTask;
            },
            cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        if (subscription is not null) {
            await subscription.DisposeAsync();
        }
    }
}

public sealed record UserNotification(Guid UserId, string Message, DateTime SentAtUtc);
```
