using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Postgres.Internals;

public sealed class DatabaseInitializer<T> : IHostedService where T : DbContext {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer<T>> _logger;

    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer<T>> logger) {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<T>();
        var hasMigrations = dbContext.Database.GetMigrations().Any();

        if (hasMigrations) {
            await dbContext.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Applied migrations for {dbContext}", typeof(T).Name);
            return;
        }

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        _logger.LogInformation("Ensured database is created for {dbContext}", typeof(T).Name);
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
