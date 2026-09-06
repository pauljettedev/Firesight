using Firesight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Firesight.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", async (FiresightDbContext dbContext) =>
        {
            var databaseAvailable = await dbContext.Database.CanConnectAsync();

            return Results.Ok(new
            {
                status = "ok",
                database = databaseAvailable ? "connected" : "unavailable"
            });
        });

        return endpoints;
    }
}
