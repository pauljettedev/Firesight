using Firesight.Application.Wildfires;

namespace Firesight.Mcp.Models;

public sealed record NearbyWildfireResult(
    WildfireResult Wildfire,
    double DistanceKm)
{
    public static NearbyWildfireResult FromApplication(NearbyWildfireDto nearby) =>
        new(
            WildfireResult.FromApplication(nearby.Wildfire),
            nearby.DistanceKm);
}
