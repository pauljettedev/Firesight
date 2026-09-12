using System.Text.Json;
using Anthropic.Models.Messages;
using Firesight.Application.AskFiresight;
using Firesight.Application.Common;
using Firesight.Application.Locations;
using Firesight.Application.Wildfires;
using Microsoft.Extensions.Options;

namespace Firesight.Infrastructure.Claude;

public sealed class ClaudeAskFiresightService : IAskFiresightService
{
    private const int MaxToolRounds = 4;
    private const int MaxResponseTokens = 8000;

    private const string SystemInstructions =
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

    private static readonly Tool GeocodeLocationTool = new()
    {
        Name = "geocode_location",
        Description = "Resolve a Canadian town or city name to latitude and longitude.",
        Strict = true,
        InputSchema = new()
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["query"] = Schema(
                    """
                    {
                      "type": "string",
                      "description": "Canadian town or city, optionally including province, for example Ottawa, ON."
                    }
                    """)
            },
            Required = ["query"],
            AdditionalProperties = false
        }
    };

    private static readonly Tool GetActiveWildfiresTool = new()
    {
        Name = "get_active_wildfires",
        Description = "Query current Firesight wildfire records. Use status and responseMode to express simple factual intent.",
        Strict = true,
        InputSchema = new()
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["status"] = Schema(
                    """
                    {
                      "type": ["string", "null"],
                      "enum": ["OC", "BH", "UC", "EX", null],
                      "description": "Optional CWFIS stage-of-control filter. Use null when no status filter is requested."
                    }
                    """),
                ["responseMode"] = Schema(
                    """
                    {
                      "type": "string",
                      "enum": ["records", "count", "exists"],
                      "description": "Use records for lists/summaries, count for numeric counts, and exists for yes/no existence questions."
                    }
                    """)
            },
            Required = ["status", "responseMode"],
            AdditionalProperties = false
        }
    };

    private static readonly Tool GetWildfireByExternalIdTool = new()
    {
        Name = "get_wildfire_by_external_id",
        Description = "Return a current wildfire by its CWFIS national fire identifier.",
        Strict = true,
        InputSchema = new()
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["externalId"] = Schema(
                    """
                    {
                      "type": "string",
                      "description": "CWFIS national fire identifier, for example 2026_ON_THU_FIRE_036."
                    }
                    """),
                ["responseMode"] = Schema(
                    """
                    {
                      "type": "string",
                      "enum": ["record", "status"],
                      "description": "Use status for a direct stage-of-control question; otherwise use record."
                    }
                    """)
            },
            Required = ["externalId", "responseMode"],
            AdditionalProperties = false
        }
    };

    private static readonly Tool FindWildfiresNearLocationTool = new()
    {
        Name = "find_wildfires_near_location",
        Description = "Find current Firesight wildfire records within a radius of a latitude and longitude, ordered nearest first.",
        Strict = true,
        InputSchema = new()
        {
            Properties = new Dictionary<string, JsonElement>
            {
                ["latitude"] = Schema(
                    """
                    {
                      "type": "number",
                      "description": "Latitude in decimal degrees, from -90 to 90."
                    }
                    """),
                ["longitude"] = Schema(
                    """
                    {
                      "type": "number",
                      "description": "Longitude in decimal degrees, from -180 to 180."
                    }
                    """),
                ["radiusKm"] = Schema(
                    """
                    {
                      "type": "number",
                      "description": "Search radius in kilometres. Must be greater than zero."
                    }
                    """),
                ["status"] = Schema(
                    """
                    {
                      "type": ["string", "null"],
                      "enum": ["OC", "BH", "UC", "EX", null],
                      "description": "Optional CWFIS stage-of-control filter. Use null when no status filter is requested."
                    }
                    """),
                ["responseMode"] = Schema(
                    """
                    {
                      "type": "string",
                      "enum": ["records", "count", "exists"],
                      "description": "Use records for lists/summaries, count for numeric counts, and exists for yes/no existence questions."
                    }
                    """)
            },
            Required = ["latitude", "longitude", "radiusKm", "status", "responseMode"],
            AdditionalProperties = false
        }
    };

    private static readonly Tool GetFeedSyncStateTool = new()
    {
        Name = "get_feed_sync_state",
        Description = "Return Firesight dataset synchronization state, including the last successful fetch time.",
        Strict = true,
        InputSchema = new()
        {
            Properties = new Dictionary<string, JsonElement>(),
            Required = [],
            AdditionalProperties = false
        }
    };

    private readonly IClaudeMessagesClient client;
    private readonly string model;
    private readonly IWildfireService wildfireService;
    private readonly ILocationGeocoder locationGeocoder;

    public ClaudeAskFiresightService(
        IClaudeMessagesClient client,
        IOptions<ClaudeOptions> options,
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

        List<MessageParam> messages =
        [
            new() { Role = Role.User, Content = question.Trim() }
        ];

        var toolsUsed = new List<string>();
        var toolNamesUsed = new HashSet<string>(StringComparer.Ordinal);
        GeocodedLocationDto? lastGeocodedLocation = null;
        AskFiresightMapContext? mapContext = null;

        // Each loop iteration = one round trip to Claude. We keep going until it
        // answers with plain text instead of a tool call, or we hit MaxToolRounds.
        for (var round = 0; round < MaxToolRounds; round++)
        {
            // Send the whole conversation so far (messages), not just the new bit —
            // Claude has no memory between calls, so we resend everything every round.
            var parameters = new MessageCreateParams
            {
                Model = model,
                MaxTokens = MaxResponseTokens,
                System = SystemInstructions,
                Tools =
                [
                    GeocodeLocationTool,
                    GetActiveWildfiresTool,
                    GetWildfireByExternalIdTool,
                    FindWildfiresNearLocationTool,
                    GetFeedSyncStateTool
                ],
                Messages = messages
            };

            Message response = await client.CreateAsync(parameters, cancellationToken);

            // response.Content is a mixed bag of pieces (text, thinking, tool calls).
            // We walk through it once and sort each piece into a bucket:
            //  - assistantContent = everything we need to echo back so Claude
            //    "remembers" what it just said, if we loop again
            //  - toolResults      = what our own code found when it ran a tool
            //  - answerParts      = plain text, in case this round IS the final answer
            List<ContentBlockParam> assistantContent = [];
            List<ContentBlockParam> toolResults = [];
            List<string> answerParts = [];
            var toolUseCount = 0;

            foreach (ContentBlock block in response.Content)
            {
                if (block.TryPickText(out TextBlock? text))
                {
                    assistantContent.Add(new TextBlockParam { Text = text.Text });
                    answerParts.Add(text.Text);
                    continue;
                }

                // Claude's internal "scratchpad" reasoning. We don't use it for
                // anything, but it MUST be sent back unchanged next round or the
                // API rejects the request.
                if (block.TryPickThinking(out ThinkingBlock? thinking))
                {
                    assistantContent.Add(new ThinkingBlockParam
                    {
                        Thinking = thinking.Thinking,
                        Signature = thinking.Signature
                    });
                    continue;
                }

                if (block.TryPickRedactedThinking(out RedactedThinkingBlock? redactedThinking))
                {
                    assistantContent.Add(new RedactedThinkingBlockParam
                    {
                        Data = redactedThinking.Data
                    });
                    continue;
                }

                if (!block.TryPickToolUse(out ToolUseBlock? toolUse))
                {
                    continue;
                }

                // Claude wants to call one of our tools. Echo the request back
                // (required), then actually run it against our real services.
                toolUseCount++;
                assistantContent.Add(new ToolUseBlockParam
                {
                    ID = toolUse.ID,
                    Name = toolUse.Name,
                    Input = toolUse.Input
                });

                if (toolNamesUsed.Add(toolUse.Name))
                {
                    toolsUsed.Add(toolUse.Name);
                }

                var execution = await ExecuteToolAsync(
                    toolUse,
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

                // For count/exists/status questions, WE compute the answer from the
                // tool's real data instead of asking Claude to say it — if we can,
                // skip the rest of the loop and return right now.
                var deterministicAnswer = TryCreateDeterministicAnswer(
                    execution,
                    toolsUsed,
                    mapContext);

                if (deterministicAnswer is not null)
                {
                    return deterministicAnswer;
                }

                // Otherwise, hand the tool's result back to Claude next round.
                toolResults.Add(new ToolResultBlockParam
                {
                    ToolUseID = toolUse.ID,
                    Content = execution.Output
                });
            }

            // No tool calls at all this round means Claude gave a final answer.
            if (toolUseCount == 0)
            {
                var answer = string.Join("\n", answerParts);

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

            // Grow the conversation for the next round: what Claude said, then
            // what our tools found, and loop back to the top to ask again.
            messages.Add(new() { Role = Role.Assistant, Content = assistantContent });
            messages.Add(new() { Role = Role.User, Content = toolResults });
        }

        throw new InvalidOperationException(
            $"Ask Firesight exceeded the maximum of {MaxToolRounds} tool-call rounds.");
    }

    private async Task<ToolExecutionResult> ExecuteToolAsync(
        ToolUseBlock toolUse,
        GeocodedLocationDto? lastGeocodedLocation,
        CancellationToken cancellationToken)
    {
        try
        {
            var input = toolUse.Input;

            switch (toolUse.Name)
            {
                case "geocode_location":
                {
                    var location = await locationGeocoder.FindAsync(
                        GetString(input, "query"),
                        cancellationToken);

                    return new ToolExecutionResult(
                        JsonSerializer.Serialize(location, JsonOptions),
                        location);
                }

                case "get_active_wildfires":
                {
                    var status = GetOptionalString(input, "status");
                    var responseMode = GetString(input, "responseMode");
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
                    var responseMode = GetString(input, "responseMode");
                    var result = await wildfireService.GetWildfireByExternalIdAsync(
                        GetString(input, "externalId"),
                        cancellationToken);

                    return new ToolExecutionResult(
                        JsonSerializer.Serialize(result, JsonOptions),
                        ResponseMode: responseMode,
                        Wildfire: result);
                }

                case "find_wildfires_near_location":
                {
                    var latitude = GetDouble(input, "latitude");
                    var longitude = GetDouble(input, "longitude");
                    var radiusKm = GetDouble(input, "radiusKm");
                    var status = GetOptionalString(input, "status");
                    var responseMode = GetString(input, "responseMode");

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
                        $"Unsupported Firesight tool '{toolUse.Name}'.");
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

    private static JsonElement Schema(string json) =>
        JsonSerializer.Deserialize<JsonElement>(json);

    private static string GetString(
        IReadOnlyDictionary<string, JsonElement> input,
        string propertyName) =>
        input[propertyName].GetString()!;

    private static double GetDouble(
        IReadOnlyDictionary<string, JsonElement> input,
        string propertyName) =>
        input[propertyName].GetDouble();

    private static string? GetOptionalString(
        IReadOnlyDictionary<string, JsonElement> input,
        string propertyName)
    {
        if (!input.TryGetValue(propertyName, out var element) ||
            element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return element.GetString();
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
