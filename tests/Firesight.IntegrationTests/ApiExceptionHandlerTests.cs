using System.Net;
using System.Net.Http.Json;
using Firesight.Api.Errors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Firesight.IntegrationTests;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task ClientAbortedCancellation_Returns499WithoutProblemDetailsBody()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/test/client-aborted");

        Assert.Equal((HttpStatusCode)499, response.StatusCode);
        Assert.Equal(string.Empty, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task NonClientCancellation_Returns500ProblemDetails()
    {
        await using var app = await CreateAppAsync();
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/test/internal-cancellation");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();

        Assert.NotNull(problem);
        Assert.Equal(500, problem.Status);
        Assert.Equal("An unexpected error occurred.", problem.Title);
    }

    private static async Task<WebApplication> CreateAppAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();

        var app = builder.Build();
        app.UseExceptionHandler();

        app.MapGet("/test/client-aborted", (HttpContext context) =>
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            context.RequestAborted = cancellation.Token;
            throw new OperationCanceledException(cancellation.Token);
        });

        app.MapGet("/test/internal-cancellation", () =>
        {
            throw new OperationCanceledException("Internal operation was cancelled.");
        });

        await app.StartAsync();
        return app;
    }

    private sealed record ProblemResponse(
        string? Title,
        int? Status);
}
