namespace GoogleMaps.Api.TileCaching;

public static class TileEndpoints {
    public static IEndpointRouteBuilder MapTileEndpoints(this IEndpointRouteBuilder endpoints) {
        var group = endpoints.MapGroup("/v1/tiles");

        group.MapGet("/{z}/{x}/{y}", async (
            int z,
            int x,
            int y,
            IMapTileCacheService tileService,
            CancellationToken cancellationToken) => {
                try {
                    var tileData = await tileService.GetTileAsync(z, x, y, cancellationToken);
                    return Results.File(tileData, "image/png", $"{z}_{x}_{y}.png");
                }
                catch (Exception ex) {
                    return Results.Problem(
                        detail: ex.Message,
                        statusCode: StatusCodes.Status500InternalServerError,
                        title: "Failed to retrieve tile"
                    );
                }
            });

        return endpoints;
    }
}
