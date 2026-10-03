using Firesight.Application.Wildfires;
using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Persistence;
using Firesight.Infrastructure.Persistence.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Firesight.Infrastructure.Wildfires;

public sealed class WildfireRepository(
    FiresightDbContext dbContext,
    IOptions<WildfireRetentionOptions> retentionOptions,
    IOptions<WildfireFreshnessOptions> freshnessOptions) : IWildfireRepository
{
    private const int Wgs84Srid = 4326;

    private readonly WildfireRetentionOptions _retentionOptions = retentionOptions.Value;
    private readonly WildfireFreshnessOptions _freshnessOptions = freshnessOptions.Value;

    public async Task<IReadOnlyList<WildfireDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoffs = await GetCutoffsAsync(cancellationToken);

        var wildfires = await VisibleWildfires(cutoffs)
            .OrderByDescending(fire => fire.AreaHectares)
            .ToListAsync(cancellationToken);

        return wildfires
            .Select(fire => ToDto(fire, cutoffs.StaleCutoffUtc))
            .ToList();
    }

    public async Task<WildfireDto?> GetByExternalIdAsync(
        string externalId,
        CancellationToken cancellationToken = default)
    {
        var cutoffs = await GetCutoffsAsync(cancellationToken);

        var wildfire = await VisibleWildfires(cutoffs)
            .Where(fire => fire.ExternalId == externalId)
            .SingleOrDefaultAsync(cancellationToken);

        return wildfire is null
            ? null
            : ToDto(wildfire, cutoffs.StaleCutoffUtc);
    }

    public async Task<IReadOnlyList<NearbyWildfireDto>> FindNearAsync(
        double latitude,
        double longitude,
        double radiusKm,
        CancellationToken cancellationToken = default)
    {
        var cutoffs = await GetCutoffsAsync(cancellationToken);

        // Search origin in WGS 84 longitude/latitude coordinates.
        var origin = new Point(longitude, latitude) { SRID = Wgs84Srid };

        // PostGIS geography distances are measured in metres.
        var radiusMeters = radiusKm * 1000d;

        var matches = await VisibleWildfires(cutoffs)
            .Where(fire => fire.Location.IsWithinDistance(origin, radiusMeters))
            .OrderBy(fire => fire.Location.Distance(origin))
            .Select(fire => new
            {
                Wildfire = fire,

                // Keep the actual distance so it can be returned to the caller.
                DistanceMeters = fire.Location.Distance(origin)
            })
            .ToListAsync(cancellationToken);

        return matches
            .Select(match => new NearbyWildfireDto(
                ToDto(match.Wildfire, cutoffs.StaleCutoffUtc),
                match.DistanceMeters / 1000d))
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

    // The fires the app shows. Every read query starts here, so none can skip
    // the rules that hide old fires.
    private IQueryable<Wildfire> VisibleWildfires(Cutoffs cutoffs)
    {
        var query = dbContext.Wildfires
            .AsNoTracking()
            .Where(WildfireRules.IsNotExtinguishedBefore(cutoffs.ExtinguishedCutoffUtc));

        // No successful sync yet, so no fire can be missing from the feed.
        if (cutoffs.MissingFromFeedCutoffUtc is not null)
        {
            query = query.Where(
                WildfireRules.IsSeenInFeedSince(cutoffs.MissingFromFeedCutoffUtc.Value));
        }

        return query;
    }

    private async Task<Cutoffs> GetCutoffsAsync(CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        // "Missing from the feed" is counted back from the last time the feed
        // was read, not from now. If CWFIS is down, fires aren't missing;
        // Firesight just hasn't been able to look. Counting from now would
        // hide every fire after a few days of outage.
        var lastSuccessfulFetchUtc = await dbContext.CwfisSyncStates
            .AsNoTracking()
            .Where(state => state.Source == CwfisSyncState.SourceKey)
            .Select(state => state.LastSuccessfulFetchUtc)
            .SingleOrDefaultAsync(cancellationToken);

        DateTime? missingFromFeedCutoffUtc = null;
        if (lastSuccessfulFetchUtc is not null)
        {
            missingFromFeedCutoffUtc =
                lastSuccessfulFetchUtc.Value.AddDays(-_retentionOptions.MissingFromFeedDays);
        }

        return new Cutoffs(
            ExtinguishedCutoffUtc: nowUtc.AddDays(-_retentionOptions.ExtinguishedDays),
            MissingFromFeedCutoffUtc: missingFromFeedCutoffUtc,
            StaleCutoffUtc: nowUtc.AddHours(-_freshnessOptions.StaleAfterHours));
    }

    // The points in time the queries compare against, worked out once per
    // request so every query in it uses the same set.
    private sealed record Cutoffs(
        DateTime ExtinguishedCutoffUtc,
        DateTime? MissingFromFeedCutoffUtc,
        DateTime StaleCutoffUtc);

    private static WildfireDto ToDto(Wildfire fire, DateTime staleCutoffUtc) =>
        new(
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
            fire.IsStale(staleCutoffUtc));

    private static bool HasSourceChanges(
        Wildfire wildfire,
        WildfireImportRecord incoming)
    {
        return wildfire.Agency != incoming.Agency ||
               wildfire.Name != incoming.Name ||
               wildfire.Location.Y != incoming.Latitude ||
               wildfire.Location.X != incoming.Longitude ||
               wildfire.StartDate != incoming.StartDate ||
               wildfire.AreaHectares != incoming.AreaHectares ||
               wildfire.Status != incoming.Status ||
               wildfire.StatusDateUtc != wildfire.EffectiveStatusDate(incoming.StatusDateUtc);
    }

    private static void Apply(
        Wildfire wildfire,
        WildfireImportRecord incoming,
        DateTime observedAtUtc)
    {
        wildfire.Agency = incoming.Agency;
        wildfire.Name = incoming.Name;
        wildfire.Location = new Point(incoming.Longitude, incoming.Latitude) { SRID = Wgs84Srid };
        wildfire.StartDate = incoming.StartDate;
        wildfire.AreaHectares = incoming.AreaHectares;

        wildfire.RecordStatus(
            incoming.Status,
            incoming.StatusDateUtc,
            observedAtUtc);
    }
}
