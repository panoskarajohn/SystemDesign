namespace Shared.Redis.Abstractions;

public interface IRedisChannelSubscriber {
    Task<IRedisChannelSubscription> SubscribeAsync<T>(
        string channel,
        Func<T, CancellationToken, Task> onMessage,
        CancellationToken cancellationToken = default);
}
