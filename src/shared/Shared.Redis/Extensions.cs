using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Redis.Abstractions;
using Shared.Redis.Internals;
using StackExchange.Redis;

namespace Shared.Redis;

public static class Extensions {
    private const string RedisSection = "redis";

    public static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration) {
        var section = configuration.GetRequiredSection(RedisSection);
        var options = new RedisOptions();
        section.Bind(options);

        services.Configure<RedisOptions>(section);
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(options.ConnectionString));

        return services.AddRedisPubSub();
    }

    public static IServiceCollection AddRedisPubSub(this IServiceCollection services) {
        services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        services.AddSingleton<IRedisChannelPublisher, RedisChannelPublisher>();
        services.AddSingleton<IRedisChannelSubscriber, RedisChannelSubscriber>();

        return services;
    }
}
