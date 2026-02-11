using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Postgres.Internals;

namespace Shared.Postgres;

public static class Extensions {
    public static IServiceCollection AddPostgres<T>(this IServiceCollection services, IConfiguration configuration) where T : DbContext {
        var section = configuration.GetSection("postgres");

        if (!section.Exists()) {
            throw new ApplicationException("You should append the postgres configuration section to your appsettings.json file");
        }

        var options = new PostgresOptions();
        section.Bind(options);
        services.Configure<PostgresOptions>(section);

        services.AddDbContext<T>(x => x.UseNpgsql(options.ConnectionString));

        services.AddHostedService<DatabaseInitializer<T>>();
        services.AddHostedService<DataInitializer>();
        services.AddScoped<IUnitOfWork, PostgresUnitOfWork<T>>();


        return services;
    }

    public static IServiceCollection AddDataInitializer<T>(this IServiceCollection services) where T : class, IDataInitializer
        => services.AddTransient<IDataInitializer, T>();

}