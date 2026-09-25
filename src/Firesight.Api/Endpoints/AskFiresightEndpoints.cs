using Firesight.Application.AskFiresight;
using Microsoft.AspNetCore.RateLimiting;

namespace Firesight.Api.Endpoints;

public static class AskFiresightEndpoints
{
    public static IEndpointRouteBuilder MapAskFiresightEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        // Limits requests per IP address, because every question costs money
        // in Claude API usage. The policy is defined in Program.cs.
        var group = endpoints.MapGroup("/api/ask")
            .WithTags("Ask Firesight")
            .RequireRateLimiting("AskFiresight");

        group.MapPost("/", async (
            AskFiresightRequest request,
            IAskFiresightService service,
            CancellationToken cancellationToken) =>
        {
            AskFiresightQuestion.Validate(request.Question);

            var result = await service.AskAsync(
                request.Question,
                cancellationToken);

            return Results.Ok(result);
        });

        return endpoints;
    }

    public sealed record AskFiresightRequest(string? Question);
}
