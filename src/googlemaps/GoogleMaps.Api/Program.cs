using GoogleMaps.Api.Locations;
using Shared.Mongo;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddMongo(configuration);
services.AddMongoRepository<UserLocation, string>("user_locations");

var app = builder.Build();

app.MapGet("/api/health", () => "healthy");
app.MapLocationEndpoints();

app.Run();
