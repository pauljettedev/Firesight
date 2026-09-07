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
}
