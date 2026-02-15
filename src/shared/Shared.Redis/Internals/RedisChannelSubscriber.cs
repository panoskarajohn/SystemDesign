using System.Text.Json;
using Shared.Redis.Abstractions;
using StackExchange.Redis;

namespace Shared.Redis.Internals;

internal sealed class RedisChannelSubscriber : IRedisChannelSubscriber {
    private readonly IConnectionMultiplexer multiplexer;
    private readonly JsonSerializerOptions serializerOptions;

    public RedisChannelSubscriber(IConnectionMultiplexer multiplexer, JsonSerializerOptions serializerOptions) {
        this.multiplexer = multiplexer;
        this.serializerOptions = serializerOptions;
    }

    public async Task<IRedisChannelSubscription> SubscribeAsync<T>(
        string channel,
        Func<T, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        ArgumentNullException.ThrowIfNull(onMessage);
        cancellationToken.ThrowIfCancellationRequested();

        var subscriber = multiplexer.GetSubscriber();
        var queue = await subscriber.SubscribeAsync(RedisChannel.Literal(channel)).ConfigureAwait(false);

        queue.OnMessage(async message => {
            if (cancellationToken.IsCancellationRequested) {
                return;
            }

            T? payload;
            try {
                payload = JsonSerializer.Deserialize<T>(message.Message!, serializerOptions);
            }
            catch (JsonException ex) {
                throw new InvalidOperationException(
                    $"Unable to deserialize Redis message on channel '{channel}' to {typeof(T).Name}.", ex);
            }

            if (payload is null) {
                throw new InvalidOperationException(
                    $"Received null payload from Redis channel '{channel}' for type {typeof(T).Name}.");
            }

            await onMessage(payload, cancellationToken).ConfigureAwait(false);
        });

        return new RedisChannelSubscription(channel, queue);
    }
}
