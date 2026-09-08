using System.Net;
using System.Net.Http.Json;
using Firesight.Api.Endpoints;
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
    public async Task Near_WithOutOfRangeLatitude_ReturnsValidationProblem()
    {
        await using var app = await CreateAppAsync(new StubWildfireService());
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=91&longitude=-75.6972&radiusKm=25");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal("Invalid wildfire search parameters", problem.Title);
        Assert.Equal(400, problem.Status);
        Assert.Equal(
            ["Latitude must be a finite value between -90 and 90."],
            problem.Errors["latitude"]);
    }

    [Fact]
    public async Task Near_WithZeroRadius_ReturnsValidationProblem()
    {
        await using var app = await CreateAppAsync(new StubWildfireService());
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=45.4215&longitude=-75.6972&radiusKm=0");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemResponse>();
        Assert.NotNull(problem);
        Assert.Equal(
            ["Radius must be a finite value greater than 0."],
            problem.Errors["radiusKm"]);
    }

    [Fact]
    public async Task Near_WithMissingRadius_ReturnsBadRequest()
    {
        await using var app = await CreateAppAsync(new StubWildfireService());
        using var client = app.GetTestClient();

        var response = await client.GetAsync(
            "/api/wildfires/near?latitude=45.4215&longitude=-75.6972");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync(IWildfireService service)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(service);

        var app = builder.Build();
        app.MapWildfireEndpoints();
        await app.StartAsync();
        return app;
    }

    private sealed class StubWildfireService : IWildfireService
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
            CancellationToken cancellationToken = default)
        {
            if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude));
            }

            if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude));
            }

            if (!double.IsFinite(radiusKm) || radiusKm <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radiusKm));
            }

            return Task.FromResult<IReadOnlyList<NearbyWildfireDto>>([]);
        }

        public Task<WildfireFeedSyncStateDto?> GetFeedSyncStateAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WildfireFeedSyncStateDto?>(null);

        public Task<WildfireSyncResult> RefreshAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed record ValidationProblemResponse(
        string? Title,
        int? Status,
        Dictionary<string, string[]> Errors);
}
