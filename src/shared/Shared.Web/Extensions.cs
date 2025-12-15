using Figgle;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Common;
using Shared.Web.Context;
using Shared.Web.Extensions;
using Shared.Web.Middleware;

namespace Shared.Web;

public static class WebExtensions {
    private static IServiceCollection addContext(this IServiceCollection services) {
        services.AddSingleton<ContextAccessor>();
        services.AddTransient(sp => sp.GetRequiredService<ContextAccessor>().Context);
        services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        return services;
    }
    private static IServiceCollection addErrorHandling(this IServiceCollection services)
        => services
            .AddScoped<ErrorMiddleware>();

    private static IApplicationBuilder useContext(this IApplicationBuilder app) {
        app.Use((ctx, next) => {
            ctx.RequestServices.GetRequiredService<ContextAccessor>().Context = new Context.Context(ctx);
            return next();
        });
        return app;
    }

    public static IApplicationBuilder UseApplication(this IApplicationBuilder app) {
        app.useContext();
        app.UseCorrelationId();

        using var scope = app.ApplicationServices.CreateScope();
        var initializer = scope.ServiceProvider.GetServices<IInitializer>();
        var semaphore = new System.Threading.SemaphoreSlim(4);

        Task.Run(async () => {
            foreach (var initializer1 in initializer) {
                try {
                    await semaphore.WaitAsync();
                    await initializer1.InitAsync();
                }
                finally {
                    semaphore.Release();
                }
            }
        });

        return app;
    }

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration) {
        var appOptions = configuration.GetSection("app").Get<AppOptions>() ?? throw new InvalidOperationException("App options are not configured properly.");
        services = services
            .AddSingleton(appOptions)
            .AddHttpContextAccessor()
            .addContext()
            .addErrorHandling();


        var version = appOptions.DisplayVersion ? $" {appOptions.Version}" : string.Empty;
        if (appOptions.DisplayBanner) {
            Console.WriteLine(FiggleFonts.Standard.Render($"{appOptions.Name}{version}"));
        }

        return services;
    }

}
