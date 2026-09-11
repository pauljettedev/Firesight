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
        If the user asks about fires near a place without specifying a distance, use a 200 km radius.
        For wildfire facts, Firesight tool output is the only authoritative source.
        The underlying CWFIS/government feed represented by Firesight is the single source of truth for fire details.
        Do not use prior model knowledge, inferred details, outside wildfire knowledge, or unstated assumptions to supplement,
        reinterpret, correct, or override tool output. If a wildfire fact is not present in tool output, do not state it.
        If tool data conflicts with prior knowledge, always use the tool data.
        Never invent wildfire records, identifiers, statuses, sizes, locations, timestamps, or distances.

        Answer only the question the user asked. Prefer a single short sentence when that fully answers it.
        For count or yes/no questions, return the answer and count only. Do not list individual wildfire details,
        coordinates, identifiers, sizes, timestamps, feed freshness, or dataset synchronization information unless
        the user specifically asks for those details.
        Do not add headings, summaries, generic disclaimers, follow-up questions, or offers to provide more information.
        Use human-facing wildfire terminology rather than internal property names or raw field labels.
        Interpret stage-of-control codes correctly: OC is Out of Control, BH is Being Held, UC is Under Control,
        and EX is Extinguished.
        Only discuss dataset synchronization freshness when the user asks about data freshness or synchronization.
        For direct safety or evacuation questions, tell the user to verify conditions with official wildfire and
        emergency authorities.

        Encode simple factual intent in tool arguments instead of relying on prose interpretation after the tool runs.
        For yes/no existence questions, use responseMode "exists".
        For count questions, use responseMode "count".
        For list, detail, comparison, ranking, or summary questions, use responseMode "records".
        When the user asks for a specific stage of control, pass its CWFIS code in status:
        OC = Out of Control, BH = Being Held, UC = Under Control, EX = Extinguished.
        When no stage-of-control filter is requested, pass status as null.
        For wildfire-by-identifier status questions, use responseMode "status"; otherwise use "record".
        Firesight application code, not the model, determines counts, existence, and direct status answers from tool data.
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
                "Query current Firesight wildfire records. Use status and responseMode to express simple factual intent.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "status": {
                      "type": ["string", "null"],
                      "enum": ["OC", "BH", "UC", "EX", null],
                      "description": "Optional CWFIS stage-of-control filter. Use null when no status filter is requested."
                    },
                    "responseMode": {
                      "type": "string",
                      "enum": ["records", "count", "exists"],
                      "description": "Use records for lists/summaries, count for numeric counts, and exists for yes/no existence questions."
                    }
                  },
                  "required": ["status", "responseMode"],
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
                    },
                    "responseMode": {
                      "type": "string",
                      "enum": ["record", "status"],
                      "description": "Use status for a direct stage-of-control question; otherwise use record."
                    }
                  },
                  "required": ["externalId", "responseMode"],
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
                    },
                    "status": {
                      "type": ["string", "null"],
                      "enum": ["OC", "BH", "UC", "EX", null],
                      "description": "Optional CWFIS stage-of-control filter. Use null when no status filter is requested."
                    },
                    "responseMode": {
                      "type": "string",
                      "enum": ["records", "count", "exists"],
                      "description": "Use records for lists/summaries, count for numeric counts, and exists for yes/no existence questions."
                    }
                  },
                  "required": ["latitude", "longitude", "radiusKm", "status", "responseMode"],
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
        GeocodedLocationDto? lastGeocodedLocation = null;
        AskFiresightMapContext? mapContext = null;

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
                    toolsUsed,
                    mapContext);
            }

            foreach (var functionCall in functionCalls)
            {
                if (toolNamesUsed.Add(functionCall.FunctionName))
                {
                    toolsUsed.Add(functionCall.FunctionName);
                }

                var execution = await ExecuteToolAsync(
                    functionCall,
                    lastGeocodedLocation,
                    cancellationToken);

                if (execution.GeocodedLocation is not null)
                {
                    lastGeocodedLocation = execution.GeocodedLocation;
                }

                if (execution.MapContext is not null)
                {
                    mapContext = execution.MapContext;
                }

                var deterministicAnswer = TryCreateDeterministicAnswer(
                    execution,
                    toolsUsed,
                    mapContext);

                if (deterministicAnswer is not null)
                {
                    return deterministicAnswer;
                }

                inputItems.Add(
                    new FunctionCallOutputResponseItem(
                        functionCall.CallId,
                        execution.Output));
            }
        }

        throw new InvalidOperationException(
            $"Ask Firesight exceeded the maximum of {MaxToolRounds} tool-call rounds.");
    }

    private async Task<ToolExecutionResult> ExecuteToolAsync(
        FunctionCallResponseItem functionCall,
        GeocodedLocationDto? lastGeocodedLocation,
        CancellationToken cancellationToken)
    {
        try
        {
            using var arguments = JsonDocument.Parse(
                functionCall.FunctionArguments);

            var root = arguments.RootElement;

            switch (functionCall.FunctionName)
            {
                case "geocode_location":
                {
                    var location = await locationGeocoder.FindAsync(
                        root.GetProperty("query").GetString()!,
                        cancellationToken);

                    return new ToolExecutionResult(
                        JsonSerializer.Serialize(location, JsonOptions),
                        location);
                }

                case "get_active_wildfires":
                {
                    var status = GetOptionalString(root, "status");
                    var responseMode = root.GetProperty("responseMode").GetString()!;
                    var result = await wildfireService.GetActiveWildfiresAsync(
                        cancellationToken);

                    return new ToolExecutionResult(
                        JsonSerializer.Serialize(result, JsonOptions),
                        RequestedStatus: status,
                        ResponseMode: responseMode,
                        Wildfires: result);
                }

                case "get_wildfire_by_external_id":
                {
                    var responseMode = root.GetProperty("responseMode").GetString()!;
                    var result = await wildfireService.GetWildfireByExternalIdAsync(
                        root.GetProperty("externalId").GetString()!,
                        cancellationToken);

                    return new ToolExecutionResult(
                        JsonSerializer.Serialize(result, JsonOptions),
                        ResponseMode: responseMode,
                        Wildfire: result);
                }

                case "find_wildfires_near_location":
                {
                    var latitude = root.GetProperty("latitude").GetDouble();
                    var longitude = root.GetProperty("longitude").GetDouble();
                    var radiusKm = root.GetProperty("radiusKm").GetDouble();
                    var status = GetOptionalString(root, "status");
                    var responseMode = root.GetProperty("responseMode").GetString()!;

                    var result = await wildfireService.FindWildfiresNearAsync(
                        latitude,
                        longitude,
                        radiusKm,
                        cancellationToken);

                    var label =
                        lastGeocodedLocation is not null &&
                        Math.Abs(lastGeocodedLocation.Latitude - latitude) < 0.001 &&
                        Math.Abs(lastGeocodedLocation.Longitude - longitude) < 0.001
                            ? lastGeocodedLocation.DisplayName
                            : null;

                    return new ToolExecutionResult(
                        JsonSerializer.Serialize(result, JsonOptions),
                        MapContext: new AskFiresightMapContext(
                            latitude,
                            longitude,
                            radiusKm,
                            label),
                        RequestedStatus: status,
                        ResponseMode: responseMode,
                        NearbyWildfires: result);
                }

                case "get_feed_sync_state":
                {
                    var result = await wildfireService.GetFeedSyncStateAsync(
                        cancellationToken);

                    return new ToolExecutionResult(
                        JsonSerializer.Serialize(result, JsonOptions));
                }

                default:
                    throw new InvalidOperationException(
                        $"Unsupported Firesight tool '{functionCall.FunctionName}'.");
            }
        }
        catch (ApplicationValidationException exception)
        {
            return new ToolExecutionResult(
                JsonSerializer.Serialize(
                    new
                    {
                        error = "Firesight rejected the tool arguments.",
                        validationErrors = exception.Errors
                    },
                    JsonOptions));
        }
    }

    private static AskFiresightResult? TryCreateDeterministicAnswer(
        ToolExecutionResult execution,
        IReadOnlyList<string> toolsUsed,
        AskFiresightMapContext? mapContext)
    {
        if (execution.ResponseMode is "count" or "exists")
        {
            if (execution.NearbyWildfires is not null && mapContext is not null)
            {
                var count = CountByStatus(
                    execution.NearbyWildfires.Select(item => item.Wildfire),
                    execution.RequestedStatus);

                return new AskFiresightResult(
                    FormatCountAnswer(
                        count,
                        execution.RequestedStatus,
                        execution.ResponseMode == "exists",
                        mapContext),
                    toolsUsed,
                    mapContext);
            }

            if (execution.Wildfires is not null)
            {
                var count = CountByStatus(
                    execution.Wildfires,
                    execution.RequestedStatus);

                return new AskFiresightResult(
                    FormatCountAnswer(
                        count,
                        execution.RequestedStatus,
                        execution.ResponseMode == "exists"),
                    toolsUsed,
                    mapContext);
            }
        }

        if (execution.ResponseMode == "status" &&
            execution.Wildfire is not null)
        {
            return new AskFiresightResult(
                $"{execution.Wildfire.ExternalId} is {StageOfControlLabel(execution.Wildfire.Status)}.",
                toolsUsed,
                mapContext);
        }

        return null;
    }

    private static string? GetOptionalString(
        JsonElement root,
        string propertyName)
    {
        var property = root.GetProperty(propertyName);

        return property.ValueKind == JsonValueKind.Null
            ? null
            : property.GetString();
    }

    private static int CountByStatus(
        IEnumerable<WildfireDto> wildfires,
        string? requestedStatus) =>
        requestedStatus is null
            ? wildfires.Count()
            : wildfires.Count(
                wildfire => string.Equals(
                    wildfire.Status,
                    requestedStatus,
                    StringComparison.OrdinalIgnoreCase));

    private static string FormatCountAnswer(
        int count,
        string? requestedStatus,
        bool yesNo,
        AskFiresightMapContext? mapContext = null)
    {
        var qualifier = requestedStatus is null
            ? string.Empty
            : $"{StageOfControlLabel(requestedStatus)} ";

        var fireLabel = count == 1 ? "wildfire" : "wildfires";
        var countText = count == 0
            ? $"no {qualifier}{fireLabel}"
            : $"{count} {qualifier}{fireLabel}";

        var scope = mapContext is null
            ? "in the current dataset"
            : $"within {Math.Round(mapContext.RadiusKm):N0} km of {ShortLocationLabel(mapContext.Label)}";

        if (!yesNo)
        {
            return $"Firesight shows {countText} {scope}.";
        }

        var prefix = count == 0 ? "No." : "Yes.";
        return $"{prefix} Firesight shows {countText} {scope}.";
    }

    private static string ShortLocationLabel(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            return "the selected location";
        }

        return label.Split(',', 2)[0].Trim();
    }

    private static string StageOfControlLabel(string status) =>
        status.ToUpperInvariant() switch
        {
            "OC" => "out-of-control",
            "BH" => "being held",
            "UC" => "under control",
            "EX" => "extinguished",
            _ => status
        };

    private sealed record ToolExecutionResult(
        string Output,
        GeocodedLocationDto? GeocodedLocation = null,
        AskFiresightMapContext? MapContext = null,
        string? RequestedStatus = null,
        string? ResponseMode = null,
        IReadOnlyList<WildfireDto>? Wildfires = null,
        IReadOnlyList<NearbyWildfireDto>? NearbyWildfires = null,
        WildfireDto? Wildfire = null);
}

#pragma warning restore OPENAI001
