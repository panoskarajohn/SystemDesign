using Microsoft.EntityFrameworkCore;
using RedisPubSub.Persistence;
using RedisPubSub.Workers;
using Shared.Postgres;
using Shared.Redis;

var builder = Host.CreateApplicationBuilder(args);

var postgresSection = builder.Configuration.GetRequiredSection("postgres");
var postgresOptions = new PostgresOptions();
postgresSection.Bind(postgresOptions);
builder.Services.AddDbContext<RedisPubSubDbContext>(options => options.UseNpgsql(postgresOptions.ConnectionString));
builder.Services.AddRedis(builder.Configuration);
builder.Services.AddHostedService<UserLocationUpdatedWorker>();

var host = builder.Build();
await host.RunAsync();
