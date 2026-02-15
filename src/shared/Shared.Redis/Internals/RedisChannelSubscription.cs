using Shared.Redis.Abstractions;
using StackExchange.Redis;

namespace Shared.Redis.Internals;

internal sealed class RedisChannelSubscription : IRedisChannelSubscription {
    private readonly ChannelMessageQueue queue;

    public RedisChannelSubscription(string channel, ChannelMessageQueue queue) {
        Channel = channel;
        this.queue = queue;
    }

    public string Channel { get; }

    public async ValueTask DisposeAsync() {
        await queue.UnsubscribeAsync().ConfigureAwait(false);
    }
}
