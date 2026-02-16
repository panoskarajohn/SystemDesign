using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NearbyFriends.Api.Persistence;
using Shared.Redis;
using Shared.Redis.Abstractions;
using Shared.Redis.Contracts;
using StackExchange.Redis;

namespace NearbyFriends.Api.Users;

public static class UserEndpoints {
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/api/users");

        group.MapPost("", async (CreateUserRequest request, NearbyFriendsDbContext dbContext, CancellationToken cancellationToken) => {
            var username = normalizeUsername(request.Username);
            var displayName = normalizeDisplayName(request.DisplayName);

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName)) {
                return Results.BadRequest(new { message = "Username and displayName are required." });
            }

            var exists = await dbContext.Users.AnyAsync(x => x.Username == username, cancellationToken);
            if (exists) {
                return Results.Conflict(new { message = "Username already exists." });
            }

            var now = DateTime.UtcNow;
            var user = new User {
                Id = Guid.NewGuid(),
                Username = username,
                DisplayName = displayName,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/users/{user.Id}", toResponse(user, Array.Empty<Guid>()));
        }).WithName("CreateUser");

        group.MapGet("{userId:guid}", async (Guid userId, NearbyFriendsDbContext dbContext, CancellationToken cancellationToken) => {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user is null) {
                return Results.NotFound();
            }

            var friendIds = await getFriendIdsAsync(userId, dbContext, cancellationToken);
            return Results.Ok(toResponse(user, friendIds));
        }).WithName("GetUser");

        group.MapPut("{userId:guid}", async (Guid userId, UpdateUserRequest request, NearbyFriendsDbContext dbContext, CancellationToken cancellationToken) => {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user is null) {
                return Results.NotFound();
            }

            var username = normalizeUsername(request.Username);
            var displayName = normalizeDisplayName(request.DisplayName);
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName)) {
                return Results.BadRequest(new { message = "Username and displayName are required." });
            }

            var usernameExists = await dbContext.Users.AnyAsync(x => x.Username == username && x.Id != userId, cancellationToken);
            if (usernameExists) {
                return Results.Conflict(new { message = "Username already exists." });
            }

            user.Username = username;
            user.DisplayName = displayName;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);

            var friendIds = await getFriendIdsAsync(userId, dbContext, cancellationToken);
            return Results.Ok(toResponse(user, friendIds));
        }).WithName("UpdateUser");

        group.MapPost("{userId:guid}/friends/{friendId:guid}", async (Guid userId, Guid friendId, NearbyFriendsDbContext dbContext, CancellationToken cancellationToken) => {
            if (userId == friendId) {
                return Results.BadRequest(new { message = "A user cannot add themselves as a friend." });
            }

            var users = await dbContext.Users
                .Where(x => x.Id == userId || x.Id == friendId)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (!users.Contains(userId) || !users.Contains(friendId)) {
                return Results.NotFound(new { message = "Both users must exist before adding friendship." });
            }

            var alreadyFriends = await dbContext.UserFriends.AnyAsync(x => x.UserId == userId && x.FriendId == friendId, cancellationToken);
            if (alreadyFriends) {
                return Results.Conflict(new { message = "Users are already friends." });
            }

            var now = DateTime.UtcNow;
            dbContext.UserFriends.AddRange(
                new UserFriend { UserId = userId, FriendId = friendId, CreatedAtUtc = now },
                new UserFriend { UserId = friendId, FriendId = userId, CreatedAtUtc = now }
            );
            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Created($"/api/users/{userId}", new { userId, friendId });
        }).WithName("AddFriend");

        group.MapDelete("{userId:guid}", async (Guid userId, NearbyFriendsDbContext dbContext, CancellationToken cancellationToken) => {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
            if (user is null) {
                return Results.NotFound();
            }

            dbContext.Users.Remove(user);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Results.NoContent();
        }).WithName("DeleteUser");

        group.MapPost("{userId:guid}/location", async (
            Guid userId,
            UpdateUserLocationRequest request,
            NearbyFriendsDbContext dbContext,
            IConnectionMultiplexer multiplexer,
            IRedisChannelPublisher publisher,
            CancellationToken cancellationToken) => {
            var userExists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
            if (!userExists) {
                return Results.NotFound(new { message = "User not found." });
            }

            if (request.Latitude is < -90 or > 90) {
                return Results.BadRequest(new { message = "Latitude must be between -90 and 90." });
            }

            if (request.Longitude is < -180 or > 180) {
                return Results.BadRequest(new { message = "Longitude must be between -180 and 180." });
            }

            var message = new UserLocationUpdatedMessage(userId, request.Latitude, request.Longitude, DateTime.UtcNow);
            var key = $"users:location:{userId}";

            var redisDatabase = multiplexer.GetDatabase();
            await redisDatabase.StringSetAsync(key, JsonSerializer.Serialize(message)).ConfigureAwait(false);

            var publishedToSubscribers = await publisher
                .PublishAsync(RedisChannels.UserLocationUpdated, message, cancellationToken)
                .ConfigureAwait(false);

            var response = new UpdateUserLocationResponse(
                userId,
                request.Latitude,
                request.Longitude,
                message.UpdatedAtUtc,
                publishedToSubscribers);

            return Results.Accepted($"/api/users/{userId}/location", response);
        }).WithName("UpdateUserLocation");

        group.MapGet("{userId:guid}/location", async (
            Guid userId,
            NearbyFriendsDbContext dbContext,
            IConnectionMultiplexer multiplexer,
            CancellationToken cancellationToken) => {
            var userExists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
            if (!userExists) {
                return Results.NotFound(new { message = "User not found." });
            }

            var key = $"users:location:{userId}";
            var redisDatabase = multiplexer.GetDatabase();
            var locationJson = await redisDatabase.StringGetAsync(key).ConfigureAwait(false);

            if (locationJson.IsNullOrEmpty) {
                return Results.NotFound(new { message = "Location not found." });
            }

            var message = JsonSerializer.Deserialize<UserLocationUpdatedMessage>(locationJson!);
            if (message is null) {
                return Results.Problem("Stored location payload is invalid.");
            }

            var response = new UserLocationResponse(
                message.UserId,
                message.Latitude,
                message.Longitude,
                message.UpdatedAtUtc);

            return Results.Ok(response);
        }).WithName("GetUserLocation");

        return endpoints;
    }

    private static UserResponse toResponse(User user, IReadOnlyCollection<Guid> friendIds)
        => new(user.Id, user.Username, user.DisplayName, friendIds);

    private static string normalizeUsername(string username) => username.Trim().ToLowerInvariant();

    private static string normalizeDisplayName(string displayName) => displayName.Trim();

    private static async Task<IReadOnlyCollection<Guid>> getFriendIdsAsync(Guid userId, NearbyFriendsDbContext dbContext, CancellationToken cancellationToken)
        => await dbContext.UserFriends
            .Where(x => x.UserId == userId)
            .Select(x => x.FriendId)
            .ToListAsync(cancellationToken);
}
