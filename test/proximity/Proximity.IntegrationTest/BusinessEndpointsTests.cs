using System.Net;

namespace Proximity.IntegrationTest;

[Collection("ProximityTests")]
public class BusinessEndpointsTests {
    private readonly ProximityClient _proximityClient;

    public BusinessEndpointsTests(ProximityTestFixture fixture) {
        _proximityClient = fixture.ProximityClient;
    }

    [Fact]
    public async Task CreateAndGetBusinessShouldSucceed() {
        var businessId = $"biz-{Guid.NewGuid():N}";
        var request = new CreateBusinessRequest(
            businessId,
            "1 Main St",
            "Denver",
            "CO",
            "USA",
            39.7392,
            -104.9903
        );

        try {
            var createResponse = await _proximityClient.CreateBusinessAsync(request);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var getResponse = await _proximityClient.GetBusinessAsync(businessId);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

            var business = await _proximityClient.ReadBusinessAsync(getResponse);
            Assert.NotNull(business);
            Assert.Equal(request.BusinessId, business!.BusinessId);
            Assert.Equal(request.Address, business.Address);
            Assert.Equal(request.City, business.City);
            Assert.Equal(request.State, business.State);
            Assert.Equal(request.Country, business.Country);
            Assert.Equal(request.Latitude, business.Latitude);
            Assert.Equal(request.Longtitude, business.Longtitude);
        }
        finally {
            await _proximityClient.DeleteBusinessAsync(businessId);
        }
    }

    [Fact]
    public async Task UpdateBusinessShouldReturnUpdatedPayload() {
        var businessId = $"biz-{Guid.NewGuid():N}";
        var createRequest = new CreateBusinessRequest(
            businessId,
            "10 Lake Rd",
            "Austin",
            "TX",
            "USA",
            30.2672,
            -97.7431
        );

        try {
            var createResponse = await _proximityClient.CreateBusinessAsync(createRequest);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            var updateRequest = new UpdateBusinessRequest(
                "11 Lake Rd",
                "Austin",
                "TX",
                "USA",
                30.2673,
                -97.7432
            );

            var updateResponse = await _proximityClient.UpdateBusinessAsync(businessId, updateRequest);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            var updated = await _proximityClient.ReadBusinessAsync(updateResponse);
            Assert.NotNull(updated);
            Assert.Equal(businessId, updated!.BusinessId);
            Assert.Equal(updateRequest.Address, updated.Address);
            Assert.Equal(updateRequest.City, updated.City);
            Assert.Equal(updateRequest.State, updated.State);
            Assert.Equal(updateRequest.Country, updated.Country);
            Assert.Equal(updateRequest.Latitude, updated.Latitude);
            Assert.Equal(updateRequest.Longtitude, updated.Longtitude);
        }
        finally {
            await _proximityClient.DeleteBusinessAsync(businessId);
        }
    }

    [Fact]
    public async Task DeleteBusinessShouldRemoveRecord() {
        var businessId = $"biz-{Guid.NewGuid():N}";
        var request = new CreateBusinessRequest(
            businessId,
            "55 Market St",
            "San Francisco",
            "CA",
            "USA",
            37.7749,
            -122.4194
        );

        var createResponse = await _proximityClient.CreateBusinessAsync(request);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var deleteResponse = await _proximityClient.DeleteBusinessAsync(businessId);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _proximityClient.GetBusinessAsync(businessId);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task SearchBusinessesShouldRespectRadius() {
        // Arrange
        var denverBusinessId = $"biz-{Guid.NewGuid():N}";
        var boulderBusinessId = $"biz-{Guid.NewGuid():N}";

        const double denverLat = 39.7392;
        const double denverLng = -104.9903;

        const double boulderLat = 40.01499;
        const double boulderLng = -105.27055;

        const string searchRadius = "2km";

        var denverBusinessRequest = new CreateBusinessRequest(
            denverBusinessId,
            "100 First St",
            "Denver",
            "CO",
            "USA",
            denverLat,
            denverLng
        );

        var boulderBusinessRequest = new CreateBusinessRequest(
            boulderBusinessId,
            "200 Second St",
            "Boulder",
            "CO",
            "USA",
            boulderLat,
            boulderLng
        );

        try {
            // Act: create test data
            var denverCreateResponse = await _proximityClient.CreateBusinessAsync(denverBusinessRequest);
            var boulderCreateResponse = await _proximityClient.CreateBusinessAsync(boulderBusinessRequest);

            Assert.Equal(HttpStatusCode.Created, denverCreateResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Created, boulderCreateResponse.StatusCode);

            // Act: search near Denver
            var searchHttpResponse = await _proximityClient.SearchBusinessesAsync(
                denverLat,
                denverLng,
                searchRadius
            );

            Assert.Equal(HttpStatusCode.OK, searchHttpResponse.StatusCode);

            var nearbyBusinesses = await _proximityClient.ReadBusinessesAsync(searchHttpResponse);

            // Assert
            Assert.Contains(nearbyBusinesses, b => b.BusinessId == denverBusinessId);
            Assert.DoesNotContain(nearbyBusinesses, b => b.BusinessId == boulderBusinessId);
        }
        finally {
            await _proximityClient.DeleteBusinessAsync(denverBusinessId);
            await _proximityClient.DeleteBusinessAsync(boulderBusinessId);
        }
    }

    [Fact]
    public async Task SearchBusinesses_GeohashBoundaryExample_AcrossPrimeMeridianStillReturnedWithinRadius() {
        // Arrange: two points ~150m apart, but on opposite sides of longitude 0 (Prime Meridian).
        var eastOfMeridianBusinessId = $"biz-{Guid.NewGuid():N}";
        var westOfMeridianBusinessId = $"biz-{Guid.NewGuid():N}";

        const double centerLat = 51.5000;

        // East of 0° lon
        const double eastLng = 0.0010;

        // West of 0° lon
        const double westLng = -0.0010;

        const string searchRadius = "1km";

        var eastRequest = new CreateBusinessRequest(
            eastOfMeridianBusinessId,
            "10 East St",
            "London",
            "N/A",
            "UK",
            centerLat,
            eastLng
        );

        var westRequest = new CreateBusinessRequest(
            westOfMeridianBusinessId,
            "20 West St",
            "London",
            "N/A",
            "UK",
            centerLat,
            westLng
        );

        try {
            var createEast = await _proximityClient.CreateBusinessAsync(eastRequest);
            var createWest = await _proximityClient.CreateBusinessAsync(westRequest);

            Assert.Equal(HttpStatusCode.Created, createEast.StatusCode);
            Assert.Equal(HttpStatusCode.Created, createWest.StatusCode);

            // Act: search from the east-side point
            var searchHttpResponse = await _proximityClient.SearchBusinessesAsync(
                centerLat,
                eastLng,
                searchRadius
            );

            Assert.Equal(HttpStatusCode.OK, searchHttpResponse.StatusCode);

            var results = await _proximityClient.ReadBusinessesAsync(searchHttpResponse);

            const int geohashPrecision = 3;

            // Assert: both should be within 1km and returned.
            Assert.Contains(results, b => b.BusinessId == eastOfMeridianBusinessId);
            Assert.Contains(results, b => b.BusinessId == westOfMeridianBusinessId);

            // (These prefixes differ even though the points are very close.)
            var eastHash4 = encodeGeohash(centerLat, eastLng, precision: geohashPrecision);
            var westHash4 = encodeGeohash(centerLat, westLng, precision: geohashPrecision);
            var (farLat, farLng) = findPointWithSameGeohashPrefix(
                baseLat: centerLat,
                baseLon: eastLng,
                precision: geohashPrecision,
                minDistanceKm: 25,
                maxDistanceKm: 200,
                stepKm: 1
            );

            var farHash4 = encodeGeohash(farLat, farLng, geohashPrecision);

            var nearDistanceKm = haversineKm(centerLat, eastLng, centerLat, westLng);
            var farDistanceKm = haversineKm(centerLat, eastLng, farLat, farLng);

            // Sanity checks: near is near; far is far
            Assert.True(nearDistanceKm < 0.5, $"Near points are {nearDistanceKm:F2}km apart");
            // -0.05 is just for precision issues farDistance will be ~25
            Assert.True(farDistanceKm >= 25 - 0.05, $"Far point is only {farDistanceKm:F2}km away");

            // Geohash boundary + false positives demonstration:
            Assert.NotEqual(eastHash4, westHash4);   // close but different cell (boundary)
            Assert.Equal(eastHash4, farHash4);
        }
        finally {
            await _proximityClient.DeleteBusinessAsync(eastOfMeridianBusinessId);
            await _proximityClient.DeleteBusinessAsync(westOfMeridianBusinessId);
        }
    }
    private static (double lat, double lon) findPointWithSameGeohashPrefix(
    double baseLat,
    double baseLon,
    int precision,
    double minDistanceKm,
    double maxDistanceKm,
    double stepKm) {
        // Walk eastward until we find a point that:
        // - shares the same geohash prefix
        // - is at least minDistanceKm away (and not beyond maxDistanceKm)
        var baseHash = encodeGeohash(baseLat, baseLon, precision);

        // Convert step in km to degrees longitude at this latitude
        double KmToLonDeg(double km)
            => km / (111.32 * Math.Cos(baseLat * Math.PI / 180.0));

        for (double dKm = minDistanceKm; dKm <= maxDistanceKm; dKm += stepKm) {
            var candidateLon = baseLon + KmToLonDeg(dKm);
            var candidateHash = encodeGeohash(baseLat, candidateLon, precision);

            if (candidateHash == baseHash) {
                return (baseLat, candidateLon);
            }
        }

        throw new InvalidOperationException(
            $"Could not find a point within {maxDistanceKm}km that shares geohash({precision}) with the base point.");
    }




    // Minimal geohash encoder for test verification
    private static readonly char[] GeoHashBase32 = "0123456789bcdefghjkmnpqrstuvwxyz".ToCharArray();

    private static string encodeGeohash(double latitude, double longitude, int precision) {
        var latMin = -90.0; var latMax = 90.0;
        var lonMin = -180.0; var lonMax = 180.0;

        int bit = 0;
        int ch = 0;
        bool even = true;

        var bits = new[] { 16, 8, 4, 2, 1 };
        var hash = new char[precision];
        int idx = 0;

        while (idx < precision) {
            if (even) {
                var mid = (lonMin + lonMax) / 2;
                if (longitude >= mid) { ch |= bits[bit]; lonMin = mid; }
                else { lonMax = mid; }
            }
            else {
                var mid = (latMin + latMax) / 2;
                if (latitude >= mid) { ch |= bits[bit]; latMin = mid; }
                else { latMax = mid; }
            }

            even = !even;

            if (bit < 4) bit++;
            else {
                hash[idx++] = GeoHashBase32[ch];
                bit = 0;
                ch = 0;
            }
        }

        return new string(hash);
    }
    private const double EarthRadiusKm = 6378.137;

    private static double haversineKm(double lat1, double lon1, double lat2, double lon2) {
        static double ToRad(double deg) => deg * Math.PI / 180.0;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Pow(Math.Sin(dLon / 2), 2);

        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));
        return EarthRadiusKm * c;
    }


}
