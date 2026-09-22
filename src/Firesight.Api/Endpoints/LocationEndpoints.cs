using Firesight.Application.Locations;
using Microsoft.AspNetCore.RateLimiting;

namespace Firesight.Api.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        // Policy defined in Program.cs — protects Nominatim from abuse via
        // this API, not our own costs (see rate limiter comment there).
        var group = endpoints.MapGroup("/api/locations")
            .WithTags("Locations")
            .RequireRateLimiting("Geocode");

        group.MapGet("/geocode", async (
            string query,
            ILocationGeocoder geocoder,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [nameof(query)] = ["A town or city is required."]
                    });
            }

            var location = await geocoder.FindAsync(query, cancellationToken);

            return location is null
                ? Results.NotFound()
                : Results.Ok(location);
        });

        return endpoints;
    }
}
