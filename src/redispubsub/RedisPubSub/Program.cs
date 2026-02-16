using Microsoft.EntityFrameworkCore;
using RedisPubSub.Persistence;
using RedisPubSub.Workers;
using Shared.Logging;
using Shared.Postgres;
using Shared.Redis;

var host = Host.CreateDefaultBuilder(args)
    .UseLogging()
    .ConfigureServices((context, services) => {
        var postgresSection = context.Configuration.GetRequiredSection("postgres");
        var postgresOptions = new PostgresOptions();
        postgresSection.Bind(postgresOptions);

        services.AddDbContext<RedisPubSubDbContext>(options => options.UseNpgsql(postgresOptions.ConnectionString));
        services.AddRedis(context.Configuration);
        services.AddHostedService<UserLocationUpdatedWorker>();
    })
    .Build();

await host.RunAsync();
