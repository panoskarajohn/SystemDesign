using Proximity.Api.Businesses;
using Shared.Logging;
using Shared.Mongo;
using Shared.Common;
using Shared.Web;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
services.AddApplication(configuration);
services.AddMongo(configuration);
services.AddMongoRepository<Business, string>("business");
services.AddInitializer<DevelopmentDatabaseInitializer>();
services.AddInitializer<BusinessIndexInitializer>();

var host = builder.Host;
host.UseLogging();

var app = builder.Build();
app.UseApplication();
app.UseLogging();

app.MapGet("/api/health", () => "healthy");
app.MapBusinessEndpoints();
app.MapBusinessSearch();

app.Run();
