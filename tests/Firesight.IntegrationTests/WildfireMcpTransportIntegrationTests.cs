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
    public async Task ToolDiscovery_EveryToolDescriptionExplainsStatusAndStaleness()
    {
        var service = new StubWildfireService();

        await using var app = await CreateAppAsync(service);
        await using var client = await CreateClientAsync(app);

        var tools = await client.ListToolsAsync();

        // Without this guidance, models state that stale fires are out.
        foreach (var tool in tools)
        {
            Assert.Contains("a stale fire may be out", tool.Description);
            Assert.Contains("UC (under control)", tool.Description);
        }
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
            nearbyHandler: (latitude, longitude, radiusKm, cancellationToken) =>
            {
                // Check the tool passed the caller's arguments through unchanged.
                Assert.Equal(45.4215, latitude);
                Assert.Equal(-75.6972, longitude);
                Assert.Equal(25, radiusKm);

                IReadOnlyList<NearbyWildfireDto> results =
                [
                    new NearbyWildfireDto(wildfire, 8.25)
                ];

                return Task.FromResult(results);
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
    public async Task RadiusTool_ReturnsEveryRequiredField_EvenWhenNull()
    {
        var wildfire = CreateWildfire("2026_ON_TEST_001", 45.5, -75.6) with
        {
            AreaHectares = null,
            StatusDateUtc = null
        };
        var service = new StubWildfireService(
            nearbyHandler: (latitude, longitude, radiusKm, cancellationToken) =>
            {
                // Whatever location is asked about, return our one test fire, 8.25 km away.
                IReadOnlyList<NearbyWildfireDto> results =
                [
                    new NearbyWildfireDto(wildfire, 8.25)
                ];

                return Task.FromResult(results);
            });

        await using var app = await CreateAppAsync(service);
        await using var client = await CreateClientAsync(app);

        // Read the required fields from the schema the server publishes, so
        // the test checks the output against the real contract.
        var tools = await client.ListToolsAsync();
        var radiusTool = tools.Single(tool => tool.Name == "find_wildfires_near_location");
        var outputSchema = GetOutputSchema(radiusTool);
        var wildfireSchema = outputSchema
            .GetProperty("items")
            .GetProperty("properties")
            .GetProperty("wildfire");

        var requiredFields = new List<string>();
        foreach (var field in wildfireSchema.GetProperty("required").EnumerateArray())
        {
            requiredFields.Add(field.GetString() ?? "");
        }

        var result = await client.CallToolAsync(
            "find_wildfires_near_location",
            new Dictionary<string, object?>
            {
                ["latitude"] = 45.4215,
                ["longitude"] = -75.6972,
                ["radiusKm"] = 25d
            });

        Assert.NotNull(result.StructuredContent);
        var items = result.StructuredContent.Value.EnumerateArray();
        var item = Assert.Single(items);
        var returned = item.GetProperty("wildfire");

        // Strict MCP clients reject a result that leaves out a required field,
        // even when the field's value is null.
        foreach (var field in requiredFields)
        {
            Assert.True(
                returned.TryGetProperty(field, out _),
                $"Required field '{field}' is missing from the tool result.");
        }

        // These fields are null in the test fire. Checking them proves the loop
        // was tested against real nulls, not fields that happened to have values.
        Assert.Equal(JsonValueKind.Null, returned.GetProperty("name").ValueKind);
        Assert.Equal(JsonValueKind.Null, returned.GetProperty("startDate").ValueKind);
        Assert.Equal(JsonValueKind.Null, returned.GetProperty("areaHectares").ValueKind);
        Assert.Equal(JsonValueKind.Null, returned.GetProperty("statusDateUtc").ValueKind);
    }

    [Fact]
    public async Task RadiusTool_WhenApplicationValidationFails_ReturnsActionableToolError()
    {
        var service = new StubWildfireService(
            nearbyHandler: (latitude, longitude, radiusKm, cancellationToken) =>
            {
                // Act as if the application layer rejected the latitude.
                var errors = new Dictionary<string, string[]>
                {
                    ["latitude"] = ["Latitude must be a finite value between -90 and 90."]
                };

                throw new ApplicationValidationException(errors);
            });

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
            nearbyHandler: (latitude, longitude, radiusKm, cancellationToken) =>
            {
                // Act as if the application layer rejected all three inputs.
                // Deliberately not alphabetical: MCP error formatting should be deterministic.
                var errors = new Dictionary<string, string[]>
                {
                    ["radiusKm"] = ["Radius must be a finite value greater than 0."],
                    ["latitude"] = ["Latitude must be a finite value between -90 and 90."],
                    ["longitude"] = ["Longitude must be a finite value between -180 and 180."]
                };

                throw new ApplicationValidationException(errors);
            });

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
            nearbyHandler: (latitude, longitude, radiusKm, cancellationToken) =>
            {
                // Act as if something unexpected failed, with sensitive text in the message.
                throw new InvalidOperationException(secret);
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

    // A fake wildfire service, so these tests don't need a database.
    // nearbyHandler lets each test decide what FindWildfiresNearAsync does:
    // it receives latitude, longitude, radiusKm and the cancellation token,
    // and returns the nearby fires (or throws). Without one, no fires are found.
    private sealed class StubWildfireService(
        Func<double, double, double, CancellationToken,
            Task<IReadOnlyList<NearbyWildfireDto>>>? nearbyHandler = null)
        : IWildfireService
    {
        public Task<IReadOnlyList<WildfireDto>> GetActiveWildfiresAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<WildfireDto> noWildfires = [];
            return Task.FromResult(noWildfires);
        }

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
            if (nearbyHandler is null)
            {
                IReadOnlyList<NearbyWildfireDto> noWildfires = [];
                return Task.FromResult(noWildfires);
            }

            return nearbyHandler(latitude, longitude, radiusKm, cancellationToken);
        }

        public Task<WildfireFeedSyncStateDto?> GetFeedSyncStateAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<WildfireFeedSyncStateDto?>(null);

        public Task<WildfireSyncResult> RefreshAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
