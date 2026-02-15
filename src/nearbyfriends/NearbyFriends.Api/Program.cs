using Nearby.Api.Persistence;
using Nearby.Api.Users;
using Shared.Postgres;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddPostgres<NearbyFriendsDbContext>(configuration);

var app = builder.Build();

app.MapGet("/api/health", () => "healthy");
app.MapUserEndpoints();

app.Run();
