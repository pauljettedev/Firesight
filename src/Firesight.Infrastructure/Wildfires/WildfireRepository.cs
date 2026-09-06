using Firesight.Application.Wildfires;
using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Firesight.Infrastructure.Wildfires;

public sealed class WildfireRepository(
    FiresightDbContext dbContext,
    IOptions<WildfireRetentionOptions> retentionOptions,
    IOptions<WildfireFreshnessOptions> freshnessOptions) : IWildfireRepository
{
    private readonly WildfireRetentionOptions _retentionOptions = retentionOptions.Value;
    private readonly WildfireFreshnessOptions _freshnessOptions = freshnessOptions.Value;

    public async Task<IReadOnlyList<WildfireDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var extinguishedCutoffUtc = nowUtc.AddDays(-_retentionOptions.ExtinguishedDays);
        var staleCutoffUtc = nowUtc.AddHours(-_freshnessOptions.StaleAfterHours);

        var wildfires = await dbContext.Wildfires
            .AsNoTracking()
            .Where(fire =>
                fire.Status != "EX" ||
                fire.FirstObservedExtinguishedUtc == null ||
                fire.FirstObservedExtinguishedUtc > extinguishedCutoffUtc)
            .OrderByDescending(fire => fire.AreaHectares)
            .ToListAsync(cancellationToken);

        return wildfires
            .Select(fire => new WildfireDto(
                fire.Id,
                fire.ExternalId,
                fire.Agency,
                fire.Name,
                fire.Location.Y,
                fire.Location.X,
                fire.StartDate,
                fire.AreaHectares,
                fire.Status,
                fire.StatusDateUtc,
                fire.LastSeenInFeedUtc,
                fire.LastSeenInFeedUtc < staleCutoffUtc))
            .ToList();
    }

    public async Task<(int Inserted, int Changed, int Observed)> SynchronizeAsync(
        IReadOnlyCollection<WildfireImportRecord> wildfires,
        CancellationToken cancellationToken = default)
    {
        if (wildfires.Count == 0)
        {
            return (0, 0, 0);
        }

        var incomingById = wildfires
            .GroupBy(fire => fire.ExternalId)
            .ToDictionary(group => group.Key, group => group.Last());

        var existing = await dbContext.Wildfires
            .ToDictionaryAsync(fire => fire.ExternalId, cancellationToken);

        var inserted = 0;
        var changed = 0;
        var observed = 0;
        var observedAtUtc = DateTime.UtcNow;

        foreach (var incoming in incomingById.Values)
        {
            if (existing.TryGetValue(incoming.ExternalId, out var wildfire))
            {
                if (HasSourceChanges(wildfire, incoming))
                {
                    changed++;
                }
                else
                {
                    observed++;
                }

                Apply(wildfire, incoming, observedAtUtc);
                continue;
            }

            wildfire = new Wildfire
            {
                Id = Guid.NewGuid(),
                ExternalId = incoming.ExternalId
            };

            Apply(wildfire, incoming, observedAtUtc);
            dbContext.Wildfires.Add(wildfire);
            inserted++;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (inserted, changed, observed);
    }

    private static bool HasSourceChanges(
        Wildfire wildfire,
        WildfireImportRecord incoming)
    {
        var effectiveStatusDateUtc = incoming.StatusDateUtc ?? wildfire.StatusDateUtc;

        return wildfire.Agency != incoming.Agency ||
               wildfire.Name != incoming.Name ||
               wildfire.Location.Y != incoming.Latitude ||
               wildfire.Location.X != incoming.Longitude ||
               wildfire.StartDate != incoming.StartDate ||
               wildfire.AreaHectares != incoming.AreaHectares ||
               wildfire.Status != incoming.Status ||
               wildfire.StatusDateUtc != effectiveStatusDateUtc;
    }

    private static void Apply(
        Wildfire wildfire,
        WildfireImportRecord incoming,
        DateTime observedAtUtc)
    {
        wildfire.Agency = incoming.Agency;
        wildfire.Name = incoming.Name;
        wildfire.Location = new Point(incoming.Longitude, incoming.Latitude) { SRID = 4326 };
        wildfire.StartDate = incoming.StartDate;
        wildfire.AreaHectares = incoming.AreaHectares;

        if (string.Equals(incoming.Status, "EX", StringComparison.OrdinalIgnoreCase))
        {
            wildfire.FirstObservedExtinguishedUtc ??= observedAtUtc;
        }
        else
        {
            wildfire.FirstObservedExtinguishedUtc = null;
        }

        wildfire.Status = incoming.Status;
        wildfire.StatusDateUtc = incoming.StatusDateUtc ?? wildfire.StatusDateUtc;
        wildfire.LastSeenInFeedUtc = observedAtUtc;
    }
}
