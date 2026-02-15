namespace Shared.Redis.Abstractions;

public interface IRedisChannelPublisher {
    Task<long> PublishAsync<T>(string channel, T payload, CancellationToken cancellationToken = default);
}
