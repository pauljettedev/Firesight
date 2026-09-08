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
            try
            {
                var wildfires = await service.FindWildfiresNearAsync(
                    latitude,
                    longitude,
                    radiusKm,
                    cancellationToken);

                return Results.Ok(wildfires);
            }
            catch (ArgumentOutOfRangeException exception)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [exception.ParamName ?? "query"] =
                            [GetValidationMessage(exception.ParamName)]
                    },
                    title: "Invalid wildfire search parameters");
            }
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

    private static string GetValidationMessage(string? parameterName) =>
        parameterName switch
        {
            "latitude" => "Latitude must be a finite value between -90 and 90.",
            "longitude" => "Longitude must be a finite value between -180 and 180.",
            "radiusKm" => "Radius must be a finite value greater than 0.",
            _ => "One or more query parameters are invalid."
        };
}
