using Firesight.Application.Locations;

namespace Firesight.Api.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/locations")
            .WithTags("Locations");

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
