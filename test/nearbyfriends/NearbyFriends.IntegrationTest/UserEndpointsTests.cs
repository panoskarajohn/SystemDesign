using System.Net;

namespace NearbyFriends.IntegrationTest;

[Collection("NearbyTests")]
public class UserEndpointsTests {
    private readonly NearbyClient _nearbyClient;

    public UserEndpointsTests(NearbyTestFixture fixture) {
        _nearbyClient = fixture.NearbyClient;
    }

    [Fact]
    public async Task CreateAndGetUserShouldSucceed() {
        Guid? userId = null;
        var createRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "Test User One"
        );
        try {
            var createResponse = await _nearbyClient.CreateUserAsync(createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdUser = await _nearbyClient.ReadUserAsync(createResponse);
            Assert.NotNull(createdUser);
            userId = createdUser!.UserId;

            var getResponse = await _nearbyClient.GetUserAsync(createdUser.UserId);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var user = await _nearbyClient.ReadUserAsync(getResponse);
            Assert.NotNull(user);
            Assert.Equal(createRequest.Username.ToLowerInvariant(), user!.Username);
            Assert.Equal(createRequest.DisplayName, user.DisplayName);
            Assert.Empty(user.FriendIds);
        }
        finally {
            if (userId.HasValue) {
                await _nearbyClient.DeleteUserAsync(userId.Value);
            }
        }
    }

    [Fact]
    public async Task UpdateUserShouldReturnUpdatedPayload() {
        Guid? userId = null;
        var createRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "Original Name"
        );
        try {
            var createResponse = await _nearbyClient.CreateUserAsync(createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdUser = await _nearbyClient.ReadUserAsync(createResponse);
            Assert.NotNull(createdUser);
            userId = createdUser!.UserId;

            var updateRequest = new UpdateUserRequest(
                $"updated-{Guid.NewGuid():N}",
                "Updated Name"
            );

            var updateResponse = await _nearbyClient.UpdateUserAsync(createdUser.UserId, updateRequest);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var updated = await _nearbyClient.ReadUserAsync(updateResponse);
            Assert.NotNull(updated);
            Assert.Equal(createdUser.UserId, updated!.UserId);
            Assert.Equal(updateRequest.Username.ToLowerInvariant(), updated.Username);
            Assert.Equal(updateRequest.DisplayName, updated.DisplayName);
        }
        finally {
            if (userId.HasValue) {
                await _nearbyClient.DeleteUserAsync(userId.Value);
            }
        }
    }

    [Fact]
    public async Task AddFriendShouldCreateBidirectionalFriendship() {
        Guid? userAId = null;
        Guid? userBId = null;
        var userARequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "User A"
        );
        var userBRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "User B"
        );
        try {
            var createAResponse = await _nearbyClient.CreateUserAsync(userARequest);
            var createBResponse = await _nearbyClient.CreateUserAsync(userBRequest);
            Assert.Equal(HttpStatusCode.Created, createAResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Created, createBResponse.StatusCode);

            var userA = await _nearbyClient.ReadUserAsync(createAResponse);
            var userB = await _nearbyClient.ReadUserAsync(createBResponse);
            Assert.NotNull(userA);
            Assert.NotNull(userB);
            userAId = userA!.UserId;
            userBId = userB!.UserId;

            var addFriendResponse = await _nearbyClient.AddFriendAsync(userA.UserId, userB.UserId);
            Assert.Equal(HttpStatusCode.Created, addFriendResponse.StatusCode);

            var getAResponse = await _nearbyClient.GetUserAsync(userA.UserId);
            var getBResponse = await _nearbyClient.GetUserAsync(userB.UserId);
            Assert.Equal(HttpStatusCode.OK, getAResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, getBResponse.StatusCode);

            var userAAfter = await _nearbyClient.ReadUserAsync(getAResponse);
            var userBAfter = await _nearbyClient.ReadUserAsync(getBResponse);
            Assert.NotNull(userAAfter);
            Assert.NotNull(userBAfter);

            Assert.Contains(userB.UserId, userAAfter!.FriendIds);
            Assert.Contains(userA.UserId, userBAfter!.FriendIds);
        }
        finally {
            if (userAId.HasValue) {
                await _nearbyClient.DeleteUserAsync(userAId.Value);
            }

            if (userBId.HasValue) {
                await _nearbyClient.DeleteUserAsync(userBId.Value);
            }
        }
    }

    [Fact]
    public async Task UpdateAndGetUserLocationShouldSucceed() {
        Guid? userId = null;
        var createRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "Location User"
        );

        try {
            var createResponse = await _nearbyClient.CreateUserAsync(createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var createdUser = await _nearbyClient.ReadUserAsync(createResponse);
            Assert.NotNull(createdUser);
            userId = createdUser!.UserId;

            var updateLocationRequest = new UpdateUserLocationRequest(37.9838, 23.7275);
            var updateLocationResponse = await _nearbyClient.UpdateUserLocationAsync(createdUser.UserId, updateLocationRequest);
            Assert.Equal(HttpStatusCode.Accepted, updateLocationResponse.StatusCode);

            var updatedLocation = await _nearbyClient.ReadUpdatedUserLocationAsync(updateLocationResponse);
            Assert.NotNull(updatedLocation);
            Assert.Equal(createdUser.UserId, updatedLocation!.UserId);
            Assert.Equal(updateLocationRequest.Latitude, updatedLocation.Latitude);
            Assert.Equal(updateLocationRequest.Longitude, updatedLocation.Longitude);

            var getLocationResponse = await _nearbyClient.GetUserLocationAsync(createdUser.UserId);
            Assert.Equal(HttpStatusCode.OK, getLocationResponse.StatusCode);

            var location = await _nearbyClient.ReadUserLocationAsync(getLocationResponse);
            Assert.NotNull(location);
            Assert.Equal(createdUser.UserId, location!.UserId);
            Assert.Equal(updateLocationRequest.Latitude, location.Latitude);
            Assert.Equal(updateLocationRequest.Longitude, location.Longitude);
        }
        finally {
            if (userId.HasValue) {
                await _nearbyClient.DeleteUserAsync(userId.Value);
            }
        }
    }

    [Fact]
    public async Task SubscribeToFriendLocationUpdatesShouldCreateBidirectionalNearbyFriendResults() {
        Guid? userAId = null;
        Guid? userBId = null;
        var userARequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "Nearby User A"
        );
        var userBRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "Nearby User B"
        );

        try {
            var createAResponse = await _nearbyClient.CreateUserAsync(userARequest);
            var createBResponse = await _nearbyClient.CreateUserAsync(userBRequest);
            Assert.Equal(HttpStatusCode.Created, createAResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Created, createBResponse.StatusCode);

            var userA = await _nearbyClient.ReadUserAsync(createAResponse);
            var userB = await _nearbyClient.ReadUserAsync(createBResponse);
            Assert.NotNull(userA);
            Assert.NotNull(userB);
            userAId = userA!.UserId;
            userBId = userB!.UserId;

            var addFriendResponse = await _nearbyClient.AddFriendAsync(userA.UserId, userB.UserId);
            Assert.Equal(HttpStatusCode.Created, addFriendResponse.StatusCode);

            var subscribeResponse = await _nearbyClient.SubscribeToFriendLocationUpdatesAsync(userA.UserId, userB.UserId);
            Assert.Equal(HttpStatusCode.Created, subscribeResponse.StatusCode);
            var subscribePayload = await _nearbyClient.ReadSubscribeToFriendLocationUpdatesResponseAsync(subscribeResponse);
            Assert.NotNull(subscribePayload);
            Assert.Equal(userA.UserId, subscribePayload!.UserId);
            Assert.Equal(userB.UserId, subscribePayload.FriendId);
            Assert.Equal(2, subscribePayload.CreatedSubscriptions);

            var userALocation = new UpdateUserLocationRequest(40.6401, 22.9444);
            var userBLocation = new UpdateUserLocationRequest(40.6408, 22.9451);
            var updateAResponse = await _nearbyClient.UpdateUserLocationAsync(userA.UserId, userALocation);
            var updateBResponse = await _nearbyClient.UpdateUserLocationAsync(userB.UserId, userBLocation);
            var updateASecondResponse = await _nearbyClient.UpdateUserLocationAsync(userA.UserId, userALocation);
            Assert.Equal(HttpStatusCode.Accepted, updateAResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, updateBResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Accepted, updateASecondResponse.StatusCode);

            var hasBothLinks = false;
            for (var attempt = 0; attempt < 40; attempt++) {
                var nearbyForAResponse = await _nearbyClient.GetNearbyFriendsAsync(userA.UserId);
                var nearbyForBResponse = await _nearbyClient.GetNearbyFriendsAsync(userB.UserId);
                Assert.Equal(HttpStatusCode.OK, nearbyForAResponse.StatusCode);
                Assert.Equal(HttpStatusCode.OK, nearbyForBResponse.StatusCode);

                var nearbyForA = await _nearbyClient.ReadNearbyFriendsResponseAsync(nearbyForAResponse);
                var nearbyForB = await _nearbyClient.ReadNearbyFriendsResponseAsync(nearbyForBResponse);
                Assert.NotNull(nearbyForA);
                Assert.NotNull(nearbyForB);

                var hasAToB = nearbyForA!.NearbyFriendIds.Contains(userB.UserId);
                var hasBToA = nearbyForB!.NearbyFriendIds.Contains(userA.UserId);
                if (hasAToB && hasBToA) {
                    hasBothLinks = true;
                    break;
                }

                await Task.Delay(250);
            }

            Assert.True(hasBothLinks, "Expected both users to appear in each other's nearby-friends endpoint.");
        }
        finally {
            if (userAId.HasValue) {
                await _nearbyClient.DeleteUserAsync(userAId.Value);
            }

            if (userBId.HasValue) {
                await _nearbyClient.DeleteUserAsync(userBId.Value);
            }
        }
    }
}
