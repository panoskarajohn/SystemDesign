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
        var createRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "Test User One"
        );

        var createResponse = await _nearbyClient.CreateUserAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUser = await _nearbyClient.ReadUserAsync(createResponse);
        Assert.NotNull(createdUser);

        var getResponse = await _nearbyClient.GetUserAsync(createdUser!.UserId);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var user = await _nearbyClient.ReadUserAsync(getResponse);
        Assert.NotNull(user);
        Assert.Equal(createRequest.Username.ToLowerInvariant(), user!.Username);
        Assert.Equal(createRequest.DisplayName, user.DisplayName);
        Assert.Empty(user.FriendIds);
    }

    [Fact]
    public async Task UpdateUserShouldReturnUpdatedPayload() {
        var createRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "Original Name"
        );

        var createResponse = await _nearbyClient.CreateUserAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var createdUser = await _nearbyClient.ReadUserAsync(createResponse);
        Assert.NotNull(createdUser);

        var updateRequest = new UpdateUserRequest(
            $"updated-{Guid.NewGuid():N}",
            "Updated Name"
        );

        var updateResponse = await _nearbyClient.UpdateUserAsync(createdUser!.UserId, updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await _nearbyClient.ReadUserAsync(updateResponse);
        Assert.NotNull(updated);
        Assert.Equal(createdUser.UserId, updated!.UserId);
        Assert.Equal(updateRequest.Username.ToLowerInvariant(), updated.Username);
        Assert.Equal(updateRequest.DisplayName, updated.DisplayName);
    }

    [Fact]
    public async Task AddFriendShouldCreateBidirectionalFriendship() {
        var userARequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "User A"
        );
        var userBRequest = new CreateUserRequest(
            $"user-{Guid.NewGuid():N}",
            "User B"
        );

        var createAResponse = await _nearbyClient.CreateUserAsync(userARequest);
        var createBResponse = await _nearbyClient.CreateUserAsync(userBRequest);
        Assert.Equal(HttpStatusCode.Created, createAResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createBResponse.StatusCode);

        var userA = await _nearbyClient.ReadUserAsync(createAResponse);
        var userB = await _nearbyClient.ReadUserAsync(createBResponse);
        Assert.NotNull(userA);
        Assert.NotNull(userB);

        var addFriendResponse = await _nearbyClient.AddFriendAsync(userA!.UserId, userB!.UserId);
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
}
