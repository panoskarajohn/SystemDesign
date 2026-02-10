using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Postgres.Internals;

internal class DataInitializer : IHostedService {
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataInitializer> _logger;

    public DataInitializer(IServiceProvider serviceProvider, ILogger<DataInitializer> logger) {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken) {
        await using AsyncServiceScope scope = _serviceProvider.CreateAsyncScope();
        var initializers = scope.ServiceProvider.GetServices<IDataInitializer>();
        foreach (var initializer in initializers) {
            try {
                _logger.LogInformation("Runnign the initializer: {initializer}", initializer.GetType().Name);
                await initializer.InitAsync();
            }
            catch (Exception e) {
                _logger.LogError(e, "Data initializer {initializer} failed", initializer.GetType().Name);
            }
            finally {
                _logger.LogInformation("Exiting the initializer: {initializer}", initializer.GetType().Name);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
