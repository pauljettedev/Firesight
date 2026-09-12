using Firesight.Application.AskFiresight;
using Microsoft.AspNetCore.RateLimiting;

namespace Firesight.Api.Endpoints;

public static class AskFiresightEndpoints
{
    private const int MaxQuestionLength = 500;

    public static IEndpointRouteBuilder MapAskFiresightEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        // Caps requests per IP (policy defined in Program.cs) — AI calls cost
        // real money per request, so this is the one endpoint that needs it.
        var group = endpoints.MapGroup("/api/ask")
            .WithTags("Ask Firesight")
            .RequireRateLimiting("AskFiresight");

        group.MapPost("/", async (
            AskFiresightRequest request,
            IAskFiresightService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Question))
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [nameof(request.Question)] =
                            ["A question is required."]
                    });
            }

            if (request.Question.Length > MaxQuestionLength)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]>
                    {
                        [nameof(request.Question)] =
                        [
                            $"Question must be {MaxQuestionLength} characters or fewer."
                        ]
                    });
            }

            var result = await service.AskAsync(
                request.Question,
                cancellationToken);

            return Results.Ok(result);
        });

        return endpoints;
    }

    public sealed record AskFiresightRequest(string Question);
}
