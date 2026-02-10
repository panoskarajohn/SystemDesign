using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Shared.Postgres.Internals;

public class PostgresUnitOfWork<T> : IUnitOfWork where T : DbContext {
    private readonly T _dbContext;
    private readonly ILogger<PostgresUnitOfWork<T>> _logger;

    public PostgresUnitOfWork(ILogger<PostgresUnitOfWork<T>> logger, T dbContext) {
        _logger = logger;
        _dbContext = dbContext;
    }
    public async Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogInformation("Database Transaction {transactionId} started", transaction.TransactionId);
        try {
            await action();
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Database Transaction {transactionId} completed", transaction.TransactionId);
        }
        catch {
            _logger.LogInformation("Database transaction {transactionId} rolling back", transaction.TransactionId);
        }
        finally {
            await transaction.DisposeAsync();
        }
    }
}