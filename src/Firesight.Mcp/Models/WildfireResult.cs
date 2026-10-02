using System.Text.Json.Serialization;
using Firesight.Application.Wildfires;

namespace Firesight.Mcp.Models;

// The MCP library leaves null fields out of tool results by default, but the
// output schema lists every field as required. Strict clients, such as Claude
// Code, reject a result with a missing field, so nulls are written explicitly.
public sealed record WildfireResult(
    Guid Id,
    string ExternalId,
    string Agency,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    string? Name,
    double Latitude,
    double Longitude,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    DateTime? StartDate,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    double? AreaHectares,
    string Status,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
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
