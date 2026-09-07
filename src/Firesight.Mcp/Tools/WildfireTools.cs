using System.ComponentModel;
using Firesight.Application.Wildfires;
using Firesight.Mcp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Firesight.Mcp.Tools;

public class WildfireTools
{
    public static async Task<IReadOnlyList<WildfireResult>> GetActiveWildfiresAsync(
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var wildfireService = services.GetRequiredService<IWildfireService>();
        var wildfires = await wildfireService.GetActiveWildfiresAsync(cancellationToken);

        return wildfires
            .Select(WildfireResult.FromApplication)
            .ToList();
    }

    public static async Task<WildfireResult?> GetWildfireByExternalIdAsync(
        [Description("The CWFIS national fire identifier, for example 2026_ON_THU_FIRE_036.")]
        string externalId,
        IServiceProvider services,
        CancellationToken cancellationToken)
    {
        var wildfireService = services.GetRequiredService<IWildfireService>();
        var wildfire = await wildfireService.GetWildfireByExternalIdAsync(
            externalId,
            cancellationToken);

        return wildfire is null
            ? null
            : WildfireResult.FromApplication(wildfire);
    }
}
