using System.Text.Json;
using Firesight.Application.Common;
using Firesight.Application.Wildfires;
using Firesight.Mcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Firesight.IntegrationTests;

public sealed class WildfireMcpTransportIntegrationTests
{
    [Fact]
    public async Task ToolDiscovery_ExposesRadiusToolWithPortableSchema()
    {
        var service = new StubWildfireService();

        await using var app = await CreateAppAsync(service);
        await using var client = await CreateClientAsync(app);

        var tools = await client.ListToolsAsync();
        var tool = Assert.Single(
            tools,
            candidate => candidate.Name == "find_wildfires_near_location");

        var inputSchema = tool.ProtocolTool.InputSchema;
        Assert.Equal("object", inputSchema.GetProperty("type").GetString());

        var properties = inputSchema.GetProperty("properties");
        Assert.True(properties.TryGetProperty("latitude", out _));
        Assert.True(properties.TryGetProperty("longitude", out _));
        Assert.True(properties.TryGetProperty("radiusKm", out _));

        Assert.NotNull(tool.ProtocolTool.OutputSchema);
        var outputSchemaText = tool.ProtocolTool.OutputSchema.Value.GetRawText();
        Assert.Contains("distanceKm", outputSchemaText, StringComparison.Ordinal);
        Assert.Contains("wildfire", outputSchemaText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ToolDiscovery_UsesPortableNullableSchemasForWildfireResults()
    {
        var service = new StubWildfireService();

        await using var app = await CreateAppAsync(service);
        await using var client = await CreateClientAsync(app);

        var tools = await client.ListToolsAsync();
        var toolsByName = tools.ToDictionary(tool => tool.Name, StringComparer.Ordinal);

        var activeSchema = GetOutputSchema(toolsByName["get_active_wildfires"]);
        var byIdSchema = GetOutputSchema(toolsByName["get_wildfire_by_external_id"]);
        var nearbySchema = GetOutputSchema(toolsByName["find_wildfires_near_location"]);

        AssertPortableWildfireObjectSchema(activeSchema.GetProperty("items"));

        var byIdWildfireSchema = byIdSchema
            .GetProperty("anyOf")
            .EnumerateArray()
            .Single(schema =>
                schema.TryGetProperty("type", out var type) &&
                type.ValueKind == JsonValueKind.String &&
                type.GetString() == "object");

        AssertPortableWildfireObjectSchema(byIdWildfireSchema);

        var nearbyWildfireSchema = nearbySchema
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty("wildfire");

        AssertPortableWildfireObjectSchema(nearbyWildfireSchema);
    }

    [Fact]
    public async Task RadiusTool_InvokedThroughMcp_ForwardsArgumentsAndReturnsStructuredContent()
    {
        var wildfire = CreateWildfire("2026_ON_TEST_001", 45.5, -75.6);
        var service = new StubWildfireService(
            nearbyHandler: (latitude, longitude, radiusKm, _) =>
            {
                Assert.Equal(45.4215, latitude);
                Assert.Equal(-75.6972, longitude);
                Assert.Equal(25, radiusKm);

                return Task.FromResult<IReadOnlyList<NearbyWildfireDto>>(
                [
                    new NearbyWildfireDto(wildfire, 8.25)
                ]);
            });

        await using var app = await CreateAppAsync(service);
        await using var client = await CreateClientAsync(app);

        var result = await client.CallToolAsync(
            "find_wildfires_near_location",
            new Dictionary<string, object?>
            {
                ["latitude"] = 45.4215,
                ["longitude"] = -75.6972,
                ["radiusKm"] = 25d
            });

        Assert.NotEqual(true, result.IsError);
        Assert.NotNull(result.StructuredContent);

        var structuredContent = result.StructuredContent.Value;
        Assert.Equal(JsonValueKind.Array, structuredContent.ValueKind);

        var item = Assert.Single(structuredContent.EnumerateArray());

        Assert.Equal(8.25, item.GetProperty("distanceKm").GetDouble());
        Assert.Equal(
            "2026_ON_TEST_001",
            item.GetProperty("wildfire").GetProperty("externalId").GetString());
    }

    [Fact]
    public async Task RadiusTool_WhenApplicationValidationFails_ReturnsActionableToolError()
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
        await using var client = await CreateClientAsync(app);

        var result = await client.CallToolAsync(
            "find_wildfires_near_location",
            new Dictionary<string, object?>
            {
                ["latitude"] = 91d,
                ["longitude"] = -75.6972,
                ["radiusKm"] = 25d
            });

        Assert.True(result.IsError);

        var errorText = Assert.Single(result.Content.OfType<TextContentBlock>()).Text;
        Assert.Contains("latitude", errorText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "between -90 and 90",
            errorText,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RadiusTool_WhenMultipleValidationFailuresOccur_ReturnsAllValidationMessages()
    {
        var service = new StubWildfireService(
            nearbyHandler: (_, _, _, _) =>
                throw new ApplicationValidationException(
                    new Dictionary<string, string[]>
                    {
                        // Deliberately not alphabetical: MCP error formatting should be deterministic.
                        ["radiusKm"] =
                            ["Radius must be a finite value greater than 0."],
                        ["latitude"] =
                            ["Latitude must be a finite value between -90 and 90."],
                        ["longitude"] =
                            ["Longitude must be a finite value between -180 and 180."]
                    }));

        await using var app = await CreateAppAsync(service);
        await using var client = await CreateClientAsync(app);

        var result = await client.CallToolAsync(
            "find_wildfires_near_location",
            new Dictionary<string, object?>
            {
                ["latitude"] = 91d,
                ["longitude"] = -181d,
                ["radiusKm"] = 0d
            });

        Assert.True(result.IsError);

        var errorText = Assert.Single(result.Content.OfType<TextContentBlock>()).Text;
        Assert.Contains(
            "Latitude must be a finite value between -90 and 90.",
            errorText,
            StringComparison.Ordinal);
        Assert.Contains(
            "Longitude must be a finite value between -180 and 180.",
            errorText,
            StringComparison.Ordinal);
        Assert.Contains(
            "Radius must be a finite value greater than 0.",
            errorText,
            StringComparison.Ordinal);

        var latitudeIndex = errorText.IndexOf("latitude:", StringComparison.Ordinal);
        var longitudeIndex = errorText.IndexOf("longitude:", StringComparison.Ordinal);
        var radiusIndex = errorText.IndexOf("radiusKm:", StringComparison.Ordinal);

        Assert.True(latitudeIndex >= 0);
        Assert.True(longitudeIndex > latitudeIndex);
        Assert.True(radiusIndex > longitudeIndex);
    }

    [Fact]
    public async Task RadiusTool_WhenUnexpectedExceptionOccurs_DoesNotLeakExceptionMessage()
    {
        const string secret = "database password should never reach the MCP client";

        var service = new StubWildfireService(
            nearbyHandler: (_, _, _, _) =>
                throw new InvalidOperationException(secret));

        await using var app = await CreateAppAsync(service);
        await using var client = await CreateClientAsync(app);

        var result = await client.CallToolAsync(
            "find_wildfires_near_location",
            new Dictionary<string, object?>
            {
                ["latitude"] = 45.4215,
                ["longitude"] = -75.6972,
                ["radiusKm"] = 25d
            });

        Assert.True(result.IsError);

        var errorText = Assert.Single(result.Content.OfType<TextContentBlock>()).Text;
        Assert.DoesNotContain(secret, errorText, StringComparison.OrdinalIgnoreCase);
    }

    private static JsonElement GetOutputSchema(McpClientTool tool) =>
        Assert.IsType<JsonElement>(tool.ProtocolTool.OutputSchema);

    private static void AssertPortableWildfireObjectSchema(JsonElement schema)
    {
        var properties = schema.GetProperty("properties");
        var required = schema
            .GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToHashSet(StringComparer.Ordinal);

        AssertPortableNullableProperty(properties.GetProperty("name"), "string");
        AssertPortableNullableProperty(properties.GetProperty("startDate"), "string");
        AssertPortableNullableProperty(properties.GetProperty("areaHectares"), "number");
        AssertPortableNullableProperty(properties.GetProperty("statusDateUtc"), "string");

        Assert.Contains("name", required);
        Assert.Contains("startDate", required);
        Assert.Contains("areaHectares", required);
        Assert.Contains("statusDateUtc", required);
    }

    private static void AssertPortableNullableProperty(
        JsonElement propertySchema,
        string valueType)
    {
        Assert.False(
            propertySchema.TryGetProperty("type", out var type) &&
            type.ValueKind == JsonValueKind.Array);

        var branches = propertySchema.GetProperty("anyOf").EnumerateArray().ToArray();
        Assert.Equal(2, branches.Length);
        Assert.Contains(
            branches,
            branch => branch.GetProperty("type").GetString() == valueType);
        Assert.Contains(
            branches,
            branch => branch.GetProperty("type").GetString() == "null");
    }

    private static async Task<WebApplication> CreateAppAsync(IWildfireService service)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton(service);
        builder.Services.AddMcpServer()
            .WithHttpTransport(options => options.Stateless = true)
            .WithFiresightTools();

        var app = builder.Build();
        app.MapMcp("/mcp");

        await app.StartAsync();
        return app;
    }

    private static async Task<McpClient> CreateClientAsync(WebApplication app)
    {
        var httpClient = app.GetTestClient();
        httpClient.Timeout = Timeout.InfiniteTimeSpan;

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions
            {
                Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
                TransportMode = HttpTransportMode.StreamableHttp,
                Name = "Firesight integration tests"
            },
            httpClient);

        return await McpClient.CreateAsync(transport);
    }

    private static WildfireDto CreateWildfire(
        string externalId,
        double latitude,
        double longitude) =>
        new(
            Guid.NewGuid(),
            externalId,
            "ON",
            null,
            latitude,
            longitude,
            null,
            12.5,
            "OC",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            false);

    private sealed class StubWildfireService(
        Func<double, double, double, CancellationToken,
            Task<IReadOnlyList<NearbyWildfireDto>>>? nearbyHandler = null)
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
            Task.FromResult<WildfireFeedSyncStateDto?>(null);

        public Task<WildfireSyncResult> RefreshAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
