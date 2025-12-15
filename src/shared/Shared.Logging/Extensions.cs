using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Shared.Logging.Options;
using Shared.Web;
using Shared.Web.Context;

namespace Shared.Logging;

public static class Extensions {
    private const string ConsoleOutputTemplate = "{Timestamp:HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}";

    private const string FileOutputTemplate =
        "{Timestamp:HH:mm:ss} [{Level:u3}] ({SourceContext}.{Method}) {Message}{NewLine}{Exception}";

    private const string AppSectionName = "app";
    private const string LoggerSectionName = "logger";

    public static IApplicationBuilder UseLogging(this IApplicationBuilder app) {
        app.Use(async (ctx, next) => {
            var logger = ctx.RequestServices.GetRequiredService<ILogger<IContext>>();
            var context = ctx.RequestServices.GetRequiredService<IContext>();
            logger.LogInformation(
                "Started processing a request [Request ID: '{RequestId}', Correlation ID: '{CorrelationId}', Trace ID: '{TraceId}', User ID: '{UserId}']...",
                context.RequestId, context.CorrelationId, context.TraceId,
                context.Identity?.IsAuthenticated ?? false ? context.Identity.Id : string.Empty);

            await next();

            logger.LogInformation(
                "Finished processing a request with status code: {StatusCode} [Request ID: '{RequestId}', Correlation ID: '{CorrelationId}', Trace ID: '{TraceId}', User ID: '{UserId}']",
                ctx.Response.StatusCode, context.RequestId, context.CorrelationId, context.TraceId,
                context.Identity?.IsAuthenticated ?? false ? context.Identity.Id : string.Empty);
        });

        return app;
    }

    public static IHostBuilder UseLogging(this IHostBuilder builder, Action<LoggerConfiguration>? configure = null,
      string loggerSectionName = LoggerSectionName,
      string appSectionName = AppSectionName)
      => builder.UseSerilog((context, loggerConfiguration) => {
          if (string.IsNullOrWhiteSpace(loggerSectionName)) {
              loggerSectionName = LoggerSectionName;
          }

          if (string.IsNullOrWhiteSpace(appSectionName)) {
              appSectionName = AppSectionName;
          }

          var appOptions = context.Configuration.GetRequiredSection(appSectionName).Get<AppOptions>()!;
          var loggerOptions = context.Configuration.GetRequiredSection(loggerSectionName).Get<LoggerOptions>()!;

          mapOptions(loggerOptions, appOptions, loggerConfiguration, context.HostingEnvironment.EnvironmentName);
          configure?.Invoke(loggerConfiguration);
      });

    private static void mapOptions(LoggerOptions loggerOptions, AppOptions appOptions,
        LoggerConfiguration loggerConfiguration, string environmentName) {
        var level = getLogEventLevel(loggerOptions.Level);

        loggerConfiguration.Enrich.FromLogContext()
            .MinimumLevel.Is(level)
            .Enrich.WithProperty("Environment", environmentName)
            .Enrich.WithProperty("Application", appOptions.Name)
            .Enrich.WithProperty("Instance", appOptions.Instance)
            .Enrich.WithProperty("Version", appOptions.Version);

        foreach (var (key, value) in loggerOptions.Tags ?? new Dictionary<string, object>()) {
            loggerConfiguration.Enrich.WithProperty(key, value);
        }

        foreach (var (key, value) in loggerOptions.Overrides ?? new Dictionary<string, string>()) {
            var logLevel = getLogEventLevel(value);
            loggerConfiguration.MinimumLevel.Override(key, logLevel);
        }

        loggerOptions.ExcludePaths?.ToList().ForEach(p => loggerConfiguration.Filter
            .ByExcluding(Matching.WithProperty<string>("RequestPath", n => n.EndsWith(p))));

        loggerOptions.ExcludeProperties?.ToList().ForEach(p => loggerConfiguration.Filter
            .ByExcluding(Matching.WithProperty(p)));

        configure(loggerConfiguration, loggerOptions);
    }

    private static void configure(LoggerConfiguration loggerConfiguration, LoggerOptions options) {
        var consoleOptions = options.Console ?? new ConsoleOptions() { Enabled = true };
        var fileOptions = options.File ?? throw new ArgumentNullException("File logging options must be provided.");

        if (consoleOptions.Enabled) {
            loggerConfiguration.WriteTo.Console(outputTemplate: ConsoleOutputTemplate);
        }

        if (fileOptions.Enabled) {
            var path = string.IsNullOrWhiteSpace(fileOptions.Path) ? "logs/logs.txt" : fileOptions.Path;
            if (!Enum.TryParse<RollingInterval>(fileOptions.Interval, true, out var interval)) {
                interval = RollingInterval.Day;
            }

            loggerConfiguration.WriteTo.File(path, rollingInterval: interval, outputTemplate: FileOutputTemplate);
        }
    }

    private static LogEventLevel getLogEventLevel(string level)
        => Enum.TryParse<LogEventLevel>(level, true, out var logLevel)
            ? logLevel
            : LogEventLevel.Information;


}
