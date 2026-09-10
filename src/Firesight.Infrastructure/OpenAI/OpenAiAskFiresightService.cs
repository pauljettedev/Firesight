using System.Text.Json;
using Firesight.Application.AskFiresight;
using Firesight.Application.Common;
using Firesight.Application.Locations;
using Firesight.Application.Wildfires;
using Microsoft.Extensions.Options;
using OpenAI.Responses;

namespace Firesight.Infrastructure.OpenAI;

#pragma warning disable OPENAI001

public sealed class OpenAiAskFiresightService : IAskFiresightService
{
    private const int MaxToolRounds = 4;

    private const string DeveloperInstructions =
        """
        You are Ask Firesight, an assistant for the Firesight Canadian wildfire demo.

        For questions about current wildfire conditions, use the supplied Firesight tools rather than guessing.
        Use geocode_location before find_wildfires_near_location when the user gives a place name instead of coordinates.
        Treat tool output as the authoritative Firesight dataset for the answer.
        Never invent wildfire records, identifiers, statuses, sizes, locations, timestamps, or distances.
        Distinguish dataset synchronization freshness from the observation timestamps on individual wildfire records.
        Firesight is a demo and not an emergency information service. When safety or evacuation decisions are involved,
        tell the user to verify information with official wildfire and emergency authorities.
        Answer clearly and concisely.
        """;

    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private static readonly ResponseTool GeocodeLocationTool =
        ResponseTool.CreateFunctionTool(
            functionName: "geocode_location",
            functionDescription:
                "Resolve a Canadian town or city name to latitude and longitude.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "query": {
                      "type": "string",
                      "description": "Canadian town or city, optionally including province, for example Ottawa, ON."
                    }
                  },
                  "required": ["query"],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true);

    private static readonly ResponseTool GetActiveWildfiresTool =
        ResponseTool.CreateFunctionTool(
            functionName: "get_active_wildfires",
            functionDescription:
                "Return the current wildfire records available in Firesight.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {},
                  "required": [],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true);

    private static readonly ResponseTool GetWildfireByExternalIdTool =
        ResponseTool.CreateFunctionTool(
            functionName: "get_wildfire_by_external_id",
            functionDescription:
                "Return a current wildfire by its CWFIS national fire identifier.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "externalId": {
                      "type": "string",
                      "description": "CWFIS national fire identifier, for example 2026_ON_THU_FIRE_036."
                    }
                  },
                  "required": ["externalId"],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true);

    private static readonly ResponseTool FindWildfiresNearLocationTool =
        ResponseTool.CreateFunctionTool(
            functionName: "find_wildfires_near_location",
            functionDescription:
                "Find current Firesight wildfire records within a radius of a latitude and longitude, ordered nearest first.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "latitude": {
                      "type": "number",
                      "description": "Latitude in decimal degrees, from -90 to 90."
                    },
                    "longitude": {
                      "type": "number",
                      "description": "Longitude in decimal degrees, from -180 to 180."
                    },
                    "radiusKm": {
                      "type": "number",
                      "description": "Search radius in kilometres. Must be greater than zero."
                    }
                  },
                  "required": ["latitude", "longitude", "radiusKm"],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true);

    private static readonly ResponseTool GetFeedSyncStateTool =
        ResponseTool.CreateFunctionTool(
            functionName: "get_feed_sync_state",
            functionDescription:
                "Return Firesight dataset synchronization state, including the last successful fetch time.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {},
                  "required": [],
                  "additionalProperties": false
                }
                """),
            strictModeEnabled: true);

    private readonly ResponsesClient client;
    private readonly string model;
    private readonly IWildfireService wildfireService;
    private readonly ILocationGeocoder locationGeocoder;

    public OpenAiAskFiresightService(
        ResponsesClient client,
        IOptions<OpenAiOptions> options,
        IWildfireService wildfireService,
        ILocationGeocoder locationGeocoder)
    {
        this.client = client;
        model = options.Value.Model;
        this.wildfireService = wildfireService;
        this.locationGeocoder = locationGeocoder;
    }

    public async Task<AskFiresightResult> AskAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        List<ResponseItem> inputItems =
        [
            ResponseItem.CreateDeveloperMessageItem(DeveloperInstructions),
            ResponseItem.CreateUserMessageItem(question.Trim())
        ];

        var toolsUsed = new List<string>();
        var toolNamesUsed = new HashSet<string>(StringComparer.Ordinal);

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var options = new CreateResponseOptions(model, inputItems)
            {
                Tools =
                {
                    GeocodeLocationTool,
                    GetActiveWildfiresTool,
                    GetWildfireByExternalIdTool,
                    FindWildfiresNearLocationTool,
                    GetFeedSyncStateTool
                }
            };

            ResponseResult response =
                await client.CreateResponseAsync(options, cancellationToken);

            inputItems.AddRange(response.OutputItems);

            var functionCalls = response.OutputItems
                .OfType<FunctionCallResponseItem>()
                .ToList();

            if (functionCalls.Count == 0)
            {
                var answer = response.GetOutputText();

                if (string.IsNullOrWhiteSpace(answer))
                {
                    throw new InvalidOperationException(
                        "Ask Firesight returned no answer.");
                }

                return new AskFiresightResult(
                    answer,
                    toolsUsed);
            }

            foreach (var functionCall in functionCalls)
            {
                if (toolNamesUsed.Add(functionCall.FunctionName))
                {
                    toolsUsed.Add(functionCall.FunctionName);
                }

                var output = await ExecuteToolAsync(
                    functionCall,
                    cancellationToken);

                inputItems.Add(
                    new FunctionCallOutputResponseItem(
                        functionCall.CallId,
                        output));
            }
        }

        throw new InvalidOperationException(
            $"Ask Firesight exceeded the maximum of {MaxToolRounds} tool-call rounds.");
    }

    private async Task<string> ExecuteToolAsync(
        FunctionCallResponseItem functionCall,
        CancellationToken cancellationToken)
    {
        try
        {
            using var arguments = JsonDocument.Parse(
                functionCall.FunctionArguments);

            var root = arguments.RootElement;

            object? result = functionCall.FunctionName switch
            {
                "geocode_location" =>
                    await locationGeocoder.FindAsync(
                        root.GetProperty("query").GetString()!,
                        cancellationToken),

                "get_active_wildfires" =>
                    await wildfireService.GetActiveWildfiresAsync(
                        cancellationToken),

                "get_wildfire_by_external_id" =>
                    await wildfireService.GetWildfireByExternalIdAsync(
                        root.GetProperty("externalId").GetString()!,
                        cancellationToken),

                "find_wildfires_near_location" =>
                    await wildfireService.FindWildfiresNearAsync(
                        root.GetProperty("latitude").GetDouble(),
                        root.GetProperty("longitude").GetDouble(),
                        root.GetProperty("radiusKm").GetDouble(),
                        cancellationToken),

                "get_feed_sync_state" =>
                    await wildfireService.GetFeedSyncStateAsync(
                        cancellationToken),

                _ => throw new InvalidOperationException(
                    $"Unsupported Firesight tool '{functionCall.FunctionName}'.")
            };

            return JsonSerializer.Serialize(result, JsonOptions);
        }
        catch (ApplicationValidationException exception)
        {
            return JsonSerializer.Serialize(
                new
                {
                    error = "Firesight rejected the tool arguments.",
                    validationErrors = exception.Errors
                },
                JsonOptions);
        }
    }
}

#pragma warning restore OPENAI001
