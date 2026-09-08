using System.Text.Json;
using Firesight.Mcp.Models;
using Firesight.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Firesight.Mcp;

public static class McpServerBuilderExtensions
{
    public static IMcpServerBuilder WithFiresightTools(this IMcpServerBuilder builder) =>
        builder
            .WithTools<WildfireTools>()
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

    private static JsonElement? GetPortableOutputSchema(string toolName) =>
        toolName switch
        {
            "get_active_wildfires" => WildfireResultSchema.ActiveWildfires,
            "get_wildfire_by_external_id" => WildfireResultSchema.WildfireById,
            "find_wildfires_near_location" => WildfireResultSchema.NearbyWildfires,
            _ => null
        };
}
