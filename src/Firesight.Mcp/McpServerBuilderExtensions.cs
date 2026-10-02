using System.Text.Json;
using System.Text.Json.Serialization;
using Firesight.Mcp.Models;
using Firesight.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;

namespace Firesight.Mcp;

public static class McpServerBuilderExtensions
{
    // The MCP library leaves null fields out of tool results by default.
    // Our output schemas list every field as required, and strict clients
    // (Claude Code, for one) reject a result with a required field missing.
    // So we always write nulls, for every tool and every field.
    // This copies the library's default settings and changes only that one.
    private static readonly JsonSerializerOptions ToolJsonOptions =
        new JsonSerializerOptions(McpJsonUtilities.DefaultOptions)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };

    public static IMcpServerBuilder WithFiresightTools(this IMcpServerBuilder builder) =>
        builder
            .WithTools<WildfireTools>(ToolJsonOptions)
            .WithRequestFilters(filters =>
            {
                filters.AddListToolsFilter(next => async (context, cancellationToken) =>
                {
                    var result = await next(context, cancellationToken);

                    foreach (var tool in result.Tools)
                    {
                        tool.OutputSchema = GetPortableOutputSchema(tool.Name)
                            ?? tool.OutputSchema;
                    }

                    return result;
                });
            });

    // Some of our tools can return null, like a wildfire that was not found.
    // By default the library describes that as type: ["string", "null"].
    // Not every app that calls MCP tools can read that format.
    // So for these three tools, we swap in a schema we wrote by hand.
    // This only changes what we tell the caller the answer looks like.
    // Our actual C# code and data don't change at all.
    private static JsonElement? GetPortableOutputSchema(string toolName) =>
        toolName switch
        {
            "get_active_wildfires" => WildfireResultSchema.ActiveWildfires,
            "get_wildfire_by_external_id" => WildfireResultSchema.WildfireById,
            "find_wildfires_near_location" => WildfireResultSchema.NearbyWildfires,
            _ => null
        };
}
