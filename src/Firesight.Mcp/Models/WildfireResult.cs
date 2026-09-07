using Firesight.Application.Wildfires;

namespace Firesight.Mcp.Models;

public sealed record WildfireResult(
    Guid Id,
    string ExternalId,
    string Agency,
    string? Name,
    double Latitude,
    double Longitude,
    DateTime? StartDate,
    double? AreaHectares,
    string Status,
    DateTime? StatusDateUtc,
    DateTime LastSeenInFeedUtc,
    bool IsStale)
{
    public static WildfireResult FromApplication(WildfireDto wildfire) =>
        new(
            wildfire.Id,
            wildfire.ExternalId,
            wildfire.Agency,
            wildfire.Name,
            wildfire.Latitude,
            wildfire.Longitude,
            wildfire.StartDate,
            wildfire.AreaHectares,
            wildfire.Status,
            wildfire.StatusDateUtc,
            wildfire.LastSeenInFeedUtc,
            wildfire.IsStale);
}
