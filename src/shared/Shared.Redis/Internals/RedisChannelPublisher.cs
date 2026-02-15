using System.Text.Json;
using Shared.Redis.Abstractions;
using StackExchange.Redis;

namespace Shared.Redis.Internals;

internal sealed class RedisChannelPublisher : IRedisChannelPublisher {
    private readonly IConnectionMultiplexer multiplexer;
    private readonly JsonSerializerOptions serializerOptions;

    public RedisChannelPublisher(IConnectionMultiplexer multiplexer, JsonSerializerOptions serializerOptions) {
        this.multiplexer = multiplexer;
        this.serializerOptions = serializerOptions;
    }

    public async Task<long> PublishAsync<T>(string channel, T payload, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        cancellationToken.ThrowIfCancellationRequested();

        var subscriber = multiplexer.GetSubscriber();
        var message = JsonSerializer.Serialize(payload, serializerOptions);

        return await subscriber.PublishAsync(RedisChannel.Literal(channel), message).ConfigureAwait(false);
    }
}
