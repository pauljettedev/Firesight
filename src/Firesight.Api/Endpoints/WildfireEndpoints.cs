using Firesight.Application.Wildfires;

namespace Firesight.Api.Endpoints;

public static class WildfireEndpoints
{
    public static IEndpointRouteBuilder MapWildfireEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/wildfires")
            .WithTags("Wildfires");

        group.MapGet("/", async (
            IWildfireService service,
            CancellationToken cancellationToken) =>
        {
            var wildfires = await service.GetActiveWildfiresAsync(cancellationToken);
            return Results.Ok(wildfires);
        });

        group.MapGet("/near", async (
            double latitude,
            double longitude,
            double radiusKm,
            IWildfireService service,
            CancellationToken cancellationToken) =>
        {
            var wildfires = await service.FindWildfiresNearAsync(
                latitude,
                longitude,
                radiusKm,
                cancellationToken);

            return Results.Ok(wildfires);
        });

        group.MapGet("/sync-state", async (
            IWildfireService service,
            CancellationToken cancellationToken) =>
        {
            var state = await service.GetFeedSyncStateAsync(cancellationToken);
            return state is null ? Results.NoContent() : Results.Ok(state);
        });

        return endpoints;
    }
}
