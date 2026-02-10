using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Shared.Postgres;

public interface IDataInitializer {
    Task InitAsync();
}

public interface IUnitOfWork {
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);
}
