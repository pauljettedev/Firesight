using Firesight.Application.Locations;
using Microsoft.AspNetCore.RateLimiting;

namespace Firesight.Api.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        // The rate limit policy is defined in Program.cs. It protects
        // Nominatim from abuse through this API, not our own costs.
        // See the comment next to that policy for more detail.
        var group = endpoints.MapGroup("/api/locations")
            .WithTags("Locations")
            .RequireRateLimiting("Geocode");

        group.MapGet("/geocode", async (
            string? query,
            ILocationGeocoder geocoder,
            CancellationToken cancellationToken) =>
        {
            LocationQuery.Validate(query);

            var location = await geocoder.FindAsync(query, cancellationToken);

            return location is null
                ? Results.NotFound()
                : Results.Ok(location);
        });

        return endpoints;
    }
}
