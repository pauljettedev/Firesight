using System.Net;
using System.Net.Http.Json;
using Firesight.Api.Endpoints;
using Firesight.Api.Errors;
using Firesight.Application.Common;
using Firesight.Application.Wildfires;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Firesight.IntegrationTests;

public sealed class WildfireEndpointsIntegrationTests
{
    [Fact]
    public async Task Near_WithValidQuery_ReturnsOk()
    {
        await using var app = await CreateAppAsync(new StubWildfireService());
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=45.4215&longitude=-75.6972&radiusKm=25");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Near_WhenApplicationValidationFails_ReturnsValidationProblem()
    {
        var service = new StubWildfireService(
            nearbyHandler: (_, _, _, _) =>
                throw new ApplicationValidationException(
                    new Dictionary<string, string[]>
                    {
                        ["latitude"] =
                            ["Latitude must be a finite value between -90 and 90."]
                    }));

        await using var app = await CreateAppAsync(service);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=91&longitude=-75.6972&radiusKm=25");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("One or more validation errors occurred.", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
        Assert.Equal(
            ["Latitude must be a finite value between -90 and 90."],
            problem.Errors["latitude"]);
    }

    [Fact]
    public async Task Near_WhenUnexpectedExceptionOccurs_ReturnsGenericServerProblem()
    {
        var service = new StubWildfireService(
            nearbyHandler: (_, _, _, _) =>
                throw new InvalidOperationException(
                    "database password should never reach the client"));

        await using var app = await CreateAppAsync(service);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=45.4215&longitude=-75.6972&radiusKm=25");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("database password", body, StringComparison.OrdinalIgnoreCase);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("An unexpected error occurred.", problem.Title);
        Assert.Equal("The request could not be completed.", problem.Detail);
        Assert.Equal(500, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    [Fact]
    public async Task Near_WhenArgumentOutOfRangeEscapesApplication_ReturnsServerError()
    {
        var service = new StubWildfireService(
            nearbyHandler: (_, _, _, _) =>
                throw new ArgumentOutOfRangeException("internalIndex"));

        await using var app = await CreateAppAsync(service);
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=45.4215&longitude=-75.6972&radiusKm=25");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Near_WithMissingRadius_ReturnsBadRequest()
    {
        await using var app = await CreateAppAsync(new StubWildfireService());
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=45.4215&longitude=-75.6972");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.TraceId));
    }

    [Fact]
    public async Task SyncState_DoesNotExposeInternalErrorField()
    {
        var syncState = new WildfireFeedSyncStateDto(
            new DateTime(2026, 9, 8, 16, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 8, 15, 0, 0, DateTimeKind.Utc),
            false,
            523,
            523,
            0);

        await using var app = await CreateAppAsync(
            new StubWildfireService(syncState: syncState));
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/api/wildfires/sync-state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("lastError", body, StringComparison.OrdinalIgnoreCase);

        var returnedState = await response.Content.ReadFromJsonAsync<WildfireFeedSyncStateDto>();
        Assert.Equal(syncState, returnedState);
    }

    [Fact]
    public async Task Sync_IsNotExposedAsPublicEndpoint()
    {
        await using var app = await CreateAppAsync(new StubWildfireService());
        using var client = app.GetTestClient();

        var response = await client.PostAsync("/api/wildfires/sync", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync(IWildfireService service)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    context.HttpContext.TraceIdentifier;
            };
        });
        builder.Services.AddExceptionHandler<ApiExceptionHandler>();
        builder.Services.AddSingleton(service);

        var app = builder.Build();
        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.MapWildfireEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class StubWildfireService(
        Func<double, double, double, CancellationToken,
            Task<IReadOnlyList<NearbyWildfireDto>>>? nearbyHandler = null,
        WildfireFeedSyncStateDto? syncState = null)
        : IWildfireService
    {
        public Task<IReadOnlyList<WildfireDto>> GetActiveWildfiresAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WildfireDto>>([]);

        public Task<WildfireDto?> GetWildfireByExternalIdAsync(
            string externalId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WildfireDto?>(null);

        public Task<IReadOnlyList<NearbyWildfireDto>> FindWildfiresNearAsync(
            double latitude,
            double longitude,
            double radiusKm,
            CancellationToken cancellationToken = default) =>
            nearbyHandler?.Invoke(latitude, longitude, radiusKm, cancellationToken)
            ?? Task.FromResult<IReadOnlyList<NearbyWildfireDto>>([]);

        public Task<WildfireFeedSyncStateDto?> GetFeedSyncStateAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(syncState);

        public Task<WildfireSyncResult> RefreshAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed record ValidationProblemResponse(
        string? Title,
        int? Status,
        Dictionary<string, string[]> Errors,
        string? TraceId);

    private sealed record ProblemResponse(
        string? Title,
        string? Detail,
        int? Status,
        string? TraceId);
}
