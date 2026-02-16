using NearbyFriends.Api.Persistence;
using NearbyFriends.Api.Users;
using Shared.Postgres;
using Shared.Redis;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services.AddPostgres<NearbyFriendsDbContext>(configuration);
services.AddRedis(configuration);

var app = builder.Build();

app.MapGet("/api/health", () => "healthy");
app.MapUserEndpoints();

app.Run();
