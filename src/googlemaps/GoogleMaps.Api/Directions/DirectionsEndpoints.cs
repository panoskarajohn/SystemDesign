using GoogleMaps.Api.Geocoding.Contracts;
using GoogleMaps.Api.Geocoding.Mongo;
using GoogleMaps.Api.Geocoding.Services;
using GoogleMaps.Api.Locations;
using MongoDB.Driver;
using Shared.Mongo.Repositories;

namespace GoogleMaps.Api.Directions;

public static class DirectionsEndpoints {
    private const double MaxEdgeDistanceMeters = 550;
    private const int NeighborsPerNode = 4;

    public static IEndpointRouteBuilder MapDirectionsEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/v1/directions");
        var routesGroup = endpoints.MapGroup("/v1/routes");

        group.MapPost("", async (
            GenerateDirectionsRequest request,
            IMongoRepository<UserLocation, string> locationRepository,
            IMongoRepository<GeoLocationDocument, string> geolocationRepository,
            CancellationToken cancellationToken) => {
                if (string.IsNullOrWhiteSpace(request.UserId)) {
                    return Results.BadRequest(new { message = "user_id is required." });
                }

                if (string.IsNullOrWhiteSpace(request.DestinationInput)) {
                    return Results.BadRequest(new { message = "destination_input is required." });
                }

                var latestLocation = await locationRepository.Collection
                    .Find(x => x.UserId == request.UserId.Trim())
                    .SortByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync(cancellationToken);

                if (latestLocation is null) {
                    return Results.NotFound(new { message = "No location found for user." });
                }

                var destination = await geolocationRepository.Collection
                    .Find(x => x.Input == request.DestinationInput.Trim())
                    .SortByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync(cancellationToken);

                if (destination is null) {
                    return Results.NotFound(new { message = "Destination geolocation not found." });
                }

                var originPlusCode = PlusCode.Encode(new GeoPoint(
                    latestLocation.Location.Coordinates.Longitude,
                    latestLocation.Location.Coordinates.Latitude
                ));
                var destinationPlusCode = destination.PlusCode.ToUpperInvariant();

                var allGeoLocations = await geolocationRepository.Collection
                    .Find(FilterDefinition<GeoLocationDocument>.Empty)
                    .ToListAsync(cancellationToken);

                var navigationNodes = allGeoLocations
                    .Select(x => new NavigationNode(
                        x.PlusCode.ToUpperInvariant(),
                        x.Input,
                        x.Location.Coordinates.Latitude,
                        x.Location.Coordinates.Longitude
                    ))
                    .ToDictionary(x => x.PlusCode, StringComparer.OrdinalIgnoreCase);

                navigationNodes[originPlusCode] = new NavigationNode(
                    originPlusCode,
                    $"user:{request.UserId.Trim()}",
                    latestLocation.Location.Coordinates.Latitude,
                    latestLocation.Location.Coordinates.Longitude
                );

                navigationNodes[destinationPlusCode] = new NavigationNode(
                    destinationPlusCode,
                    destination.Input,
                    destination.Location.Coordinates.Latitude,
                    destination.Location.Coordinates.Longitude
                );

                var adjacency = buildAdjacency(navigationNodes.Values.ToList());

                if (!tryFindShortestPath(originPlusCode, destinationPlusCode, adjacency, out var path)) {
                    return Results.NotFound(new { message = "No path found between origin and destination." });
                }

                var movementSteps = path
                    .Select((step, index) => {
                        var fromNode = navigationNodes[step.FromPlusCode];
                        var toNode = navigationNodes[step.ToPlusCode];
                        var heading = getHeading(fromNode.Latitude, fromNode.Longitude, toNode.Latitude, toNode.Longitude);

                        return new DirectionStepResponse(
                            index + 1,
                            $"Go {heading} for {Math.Round(step.DistanceMeters)} meters toward {toNode.Input}.",
                            heading,
                            step.DistanceMeters,
                            toNode.Input,
                            step.FromPlusCode,
                            step.ToPlusCode
                        );
                    })
                    .ToList();

                var orderedSteps = new List<DirectionStepResponse>(movementSteps.Count + 1);
                orderedSteps.AddRange(movementSteps);
                orderedSteps.Add(new DirectionStepResponse(
                    orderedSteps.Count + 1,
                    $"You reached {destination.Input}.",
                    "arrive",
                    0,
                    destination.Input,
                    destinationPlusCode,
                    destinationPlusCode
                ));

                var response = new GenerateDirectionsResponse(
                    request.UserId.Trim(),
                    request.DestinationInput.Trim(),
                    originPlusCode,
                    destinationPlusCode,
                    movementSteps.Sum(x => x.DistanceMeters),
                    orderedSteps
                );

                return Results.Ok(response);
            });

        routesGroup.MapPost("", async (
            GenerateRouteRequest request,
            IMongoRepository<GeoLocationDocument, string> geolocationRepository,
            CancellationToken cancellationToken) => {
                if (string.IsNullOrWhiteSpace(request.OriginInput)
                    || string.IsNullOrWhiteSpace(request.DestinationInput)) {
                    return Results.BadRequest(new { message = "origin_input and destination_input are required." });
                }

                var originInput = request.OriginInput.Trim();
                var destinationInput = request.DestinationInput.Trim();

                var origin = await geolocationRepository.Collection
                    .Find(x => x.Input == originInput)
                    .SortByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync(cancellationToken);

                if (origin is null) {
                    return Results.NotFound(new { message = "Origin geolocation not found." });
                }

                var destination = await geolocationRepository.Collection
                    .Find(x => x.Input == destinationInput)
                    .SortByDescending(x => x.Timestamp)
                    .FirstOrDefaultAsync(cancellationToken);

                if (destination is null) {
                    return Results.NotFound(new { message = "Destination geolocation not found." });
                }

                var path = new List<RoutePointResponse> {
                    new(
                        origin.Location.Coordinates.Latitude,     // Try original order
                        origin.Location.Coordinates.Longitude,
                        origin.PlusCode.ToUpperInvariant(),
                        origin.Input
                    ),
                    new(
                        destination.Location.Coordinates.Latitude,   // Try original order
                        destination.Location.Coordinates.Longitude,
                        destination.PlusCode.ToUpperInvariant(),
                        destination.Input
                    )
                };

                var totalDistanceMeters = haversineMeters(
                    origin.Location.Coordinates.Latitude,
                    origin.Location.Coordinates.Longitude,
                    destination.Location.Coordinates.Latitude,
                    destination.Location.Coordinates.Longitude
                );

                var heading = getHeading(
                    origin.Location.Coordinates.Latitude,
                    origin.Location.Coordinates.Longitude,
                    destination.Location.Coordinates.Latitude,
                    destination.Location.Coordinates.Longitude
                );

                var segments = new List<RouteSegmentResponse> {
                    new(
                        1,
                        $"Go {heading} to {destination.Input}.",
                        heading,
                        totalDistanceMeters
                    )
                };

                var drawing = new RouteDrawingResponse(
                    "polyline",
                    "Draw one polyline using the coordinates in order.",
                    path
                );

                return Results.Ok(new GenerateRouteResponse(
                    originInput,
                    destinationInput,
                    totalDistanceMeters,
                    path,
                    segments,
                    drawing
                ));
            });

        return endpoints;
    }

    private static Dictionary<string, List<NavigationEdge>> buildAdjacency(IReadOnlyList<NavigationNode> nodes) {
        var adjacency = new Dictionary<string, List<NavigationEdge>>(StringComparer.OrdinalIgnoreCase);

        foreach (var from in nodes) {
            var candidates = nodes
                .Where(to => !string.Equals(to.PlusCode, from.PlusCode, StringComparison.OrdinalIgnoreCase))
                .Select(to => new NavigationEdge(
                    from.PlusCode,
                    to.PlusCode,
                    haversineMeters(from.Latitude, from.Longitude, to.Latitude, to.Longitude)
                ))
                .Where(edge => edge.DistanceMeters <= MaxEdgeDistanceMeters)
                .OrderBy(edge => edge.DistanceMeters)
                .Take(NeighborsPerNode)
                .ToList();

            adjacency[from.PlusCode] = candidates;
        }

        return adjacency;
    }

    private static bool tryFindShortestPath(
        string originPlusCode,
        string destinationPlusCode,
        IReadOnlyDictionary<string, List<NavigationEdge>> adjacency,
        out IReadOnlyList<NavigationEdge> path) {
        path = Array.Empty<NavigationEdge>();

        if (string.Equals(originPlusCode, destinationPlusCode, StringComparison.OrdinalIgnoreCase)) {
            path = Array.Empty<NavigationEdge>();
            return true;
        }

        var queue = new PriorityQueue<string, double>();
        var distances = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase) {
            [originPlusCode] = 0
        };
        var previous = new Dictionary<string, NavigationEdge>(StringComparer.OrdinalIgnoreCase);

        queue.Enqueue(originPlusCode, 0);

        while (queue.TryDequeue(out var current, out var currentDistance)) {
            if (currentDistance > distances[current]) {
                continue;
            }

            if (string.Equals(current, destinationPlusCode, StringComparison.OrdinalIgnoreCase)) {
                break;
            }

            if (!adjacency.TryGetValue(current, out var outgoingEdges)) {
                continue;
            }

            foreach (var edge in outgoingEdges) {
                var candidateDistance = currentDistance + edge.DistanceMeters;
                if (distances.TryGetValue(edge.ToPlusCode, out var knownDistance)
                    && knownDistance <= candidateDistance) {
                    continue;
                }

                distances[edge.ToPlusCode] = candidateDistance;
                previous[edge.ToPlusCode] = edge;
                queue.Enqueue(edge.ToPlusCode, candidateDistance);
            }
        }

        if (!previous.ContainsKey(destinationPlusCode)) {
            return false;
        }

        var edgesInPath = new List<NavigationEdge>();
        var cursor = destinationPlusCode;
        while (!string.Equals(cursor, originPlusCode, StringComparison.OrdinalIgnoreCase)) {
            var edge = previous[cursor];
            edgesInPath.Add(edge);
            cursor = edge.FromPlusCode;
        }

        edgesInPath.Reverse();
        path = edgesInPath;
        return true;
    }

    private static string getHeading(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude) {
        var bearing = initialBearing(fromLatitude, fromLongitude, toLatitude, toLongitude);

        if (bearing >= 337.5 || bearing < 22.5) {
            return "north";
        }

        if (bearing < 67.5) {
            return "north-east";
        }

        if (bearing < 112.5) {
            return "east";
        }

        if (bearing < 157.5) {
            return "south-east";
        }

        if (bearing < 202.5) {
            return "south";
        }

        if (bearing < 247.5) {
            return "south-west";
        }

        if (bearing < 292.5) {
            return "west";
        }

        return "north-west";
    }

    private static double initialBearing(double fromLatitude, double fromLongitude, double toLatitude, double toLongitude) {
        var fromLat = degreesToRadians(fromLatitude);
        var toLat = degreesToRadians(toLatitude);
        var deltaLon = degreesToRadians(toLongitude - fromLongitude);

        var y = Math.Sin(deltaLon) * Math.Cos(toLat);
        var x = Math.Cos(fromLat) * Math.Sin(toLat)
                - Math.Sin(fromLat) * Math.Cos(toLat) * Math.Cos(deltaLon);

        var bearing = radiansToDegrees(Math.Atan2(y, x));
        return (bearing + 360.0) % 360.0;
    }

    private static double haversineMeters(double latitude1, double longitude1, double latitude2, double longitude2) {
        const double earthRadiusMeters = 6371000;

        var lat1 = degreesToRadians(latitude1);
        var lon1 = degreesToRadians(longitude1);
        var lat2 = degreesToRadians(latitude2);
        var lon2 = degreesToRadians(longitude2);

        var dLat = lat2 - lat1;
        var dLon = lon2 - lon1;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(lat1) * Math.Cos(lat2)
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }

    private static double degreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double radiansToDegrees(double radians) => radians * 180.0 / Math.PI;

    private sealed record NavigationNode(string PlusCode, string Input, double Latitude, double Longitude);

    private sealed record NavigationEdge(string FromPlusCode, string ToPlusCode, double DistanceMeters);
}
