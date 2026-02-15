namespace Shared.Redis.Abstractions;

public interface IRedisChannelSubscription : IAsyncDisposable {
    string Channel { get; }
}
