using Shared.Logging;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
services.AddApplication(configuration);

var host = builder.Host;
host.UseLogging();

var app = builder.Build();
app.UseApplication();
app.UseLogging();

app.MapGet("/api/health", () => "healthy");

app.Run();