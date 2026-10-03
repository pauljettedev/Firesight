using System.ComponentModel;
using Firesight.Application.Common;
using Firesight.Application.Wildfires;
using Firesight.Mcp.Models;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Firesight.Mcp.Tools;

[McpServerToolType]
public sealed class WildfireTools(IWildfireService wildfireService)
{
    // Added to every tool description, so the model reads it before using the
    // results. In testing, Claude guessed that fires missing from recent feed
    // updates had been declared out. This is the one place that explains the
    // results to the model.
    private const string ResultGuidance =
        "Each fire's status is its stage of control exactly as CWFIS reports it: " +
        "OC (out of control), BH (being held), UC (under control) or EX (extinguished). " +
        "A null field means CWFIS didn't provide that value. It isn't an error. " +
        "A fire can drop out of the CWFIS feed for reasons Firesight can't see. " +
        "isStale, or a lastSeenInFeedUtc earlier than other fires', means the record " +
        "hasn't been refreshed. It does not mean the fire is out. " +
        "Report the status as given.";

    [McpServerTool(
        Name = "get_active_wildfires",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Returns the current wildfire records available in Firesight. " + ResultGuidance)]
    public async Task<List<WildfireResult>> GetActiveWildfiresAsync(
        CancellationToken cancellationToken)
    {
        var wildfires = await wildfireService.GetActiveWildfiresAsync(cancellationToken);

        return wildfires
            .Select(WildfireResult.FromApplication)
            .ToList();
    }

    [McpServerTool(
        Name = "get_wildfire_by_external_id",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Returns a wildfire by its CWFIS national fire identifier, or null when no current record is available. " + ResultGuidance)]
    public async Task<WildfireResult?> GetWildfireByExternalIdAsync(
        [Description("The CWFIS national fire identifier, for example 2026_ON_THU_FIRE_036.")]
        string externalId,
        CancellationToken cancellationToken)
    {
        var wildfire = await wildfireService.GetWildfireByExternalIdAsync(
            externalId,
            cancellationToken);

        return wildfire is null
            ? null
            : WildfireResult.FromApplication(wildfire);
    }

    [McpServerTool(
        Name = "find_wildfires_near_location",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false,
        UseStructuredContent = true)]
    [Description("Finds current Firesight wildfire records within a radius of a latitude and longitude, ordered nearest first. " + ResultGuidance)]
    public async Task<List<NearbyWildfireResult>> FindWildfiresNearLocationAsync(
        [Description("Latitude in decimal degrees, from -90 to 90.")]
        double latitude,
        [Description("Longitude in decimal degrees, from -180 to 180.")]
        double longitude,
        [Description("Search radius in kilometres. Must be greater than zero and at most 1000.")]
        double radiusKm,
        CancellationToken cancellationToken)
    {
        try
        {
            var wildfires = await wildfireService.FindWildfiresNearAsync(
                latitude,
                longitude,
                radiusKm,
                cancellationToken);

            return wildfires
                .Select(NearbyWildfireResult.FromApplication)
                .ToList();
        }
        catch (ApplicationValidationException exception)
        {
            throw new McpException(FormatValidationErrors(exception), exception);
        }
    }

    private static string FormatValidationErrors(
        ApplicationValidationException exception) =>
        string.Join(
            " ",
            exception.Errors
                .OrderBy(error => error.Key, StringComparer.Ordinal)
                .SelectMany(
                    error => error.Value.Select(
                        message => $"{error.Key}: {message}")));
}
