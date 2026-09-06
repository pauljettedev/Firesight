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

        group.MapGet("/sync-state", async (
            IWildfireService service,
            CancellationToken cancellationToken) =>
        {
            var state = await service.GetFeedSyncStateAsync(cancellationToken);
            return state is null ? Results.NoContent() : Results.Ok(state);
        });

        group.MapPost("/sync", async (
            IWildfireService service,
            CancellationToken cancellationToken) =>
        {
            var result = await service.RefreshAsync(cancellationToken);
            return Results.Ok(result);
        });

        return endpoints;
    }
}
