using System.ComponentModel;
using Firesight.Application.Wildfires;
using ModelContextProtocol.Server;

namespace Firesight.Mcp.Tools;

[McpServerToolType]
public class WildfireTools
{
    [McpServerTool(
        Name = "get_active_wildfires",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Returns the current wildfire records available in Firesight.")]
    public static Task<IReadOnlyList<WildfireDto>> GetActiveWildfiresAsync(
        IWildfireService wildfireService,
        CancellationToken cancellationToken) =>
        wildfireService.GetActiveWildfiresAsync(cancellationToken);
}
