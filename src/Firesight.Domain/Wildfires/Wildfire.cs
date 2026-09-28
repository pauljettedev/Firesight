using NetTopologySuite.Geometries;

namespace Firesight.Domain.Wildfires;

public class Wildfire
{
    public Guid Id { get; set; }

    public string ExternalId { get; set; } = string.Empty;

    public string Agency { get; set; } = string.Empty;

    public string? Name { get; set; }

    public Point Location { get; set; } = null!;

    public DateTime? StartDate { get; set; }

    public double? AreaHectares { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? StatusDateUtc { get; set; }

    public DateTime LastSeenInFeedUtc { get; set; }

    public DateTime? FirstObservedExtinguishedUtc { get; set; }

    // Stale means Firesight hasn't seen this fire in the feed for a while. It
    // says nothing about the fire itself, and never changes the CWFIS status.
    public bool IsStale(DateTime staleCutoffUtc) =>
        LastSeenInFeedUtc < staleCutoffUtc;

    // Records the status the feed reported for this fire at observedAtUtc.
    public void RecordStatus(
        string status,
        DateTime? statusDateUtc,
        DateTime observedAtUtc)
    {
        // Exact match, the same as IsWithinRetention, which runs as SQL.
        // CWFIS status is stored exactly as sent, so the two must agree.
        if (status == WildfireRules.ExtinguishedStatus)
        {
            // Keep the first time it was seen as extinguished, so the
            // retention period isn't restarted by every sync.
            FirstObservedExtinguishedUtc ??= observedAtUtc;
        }
        else
        {
            // CWFIS status can move back from extinguished, so the clock resets.
            FirstObservedExtinguishedUtc = null;
        }

        Status = status;
        StatusDateUtc = EffectiveStatusDate(statusDateUtc);
        LastSeenInFeedUtc = observedAtUtc;
    }

    // CWFIS sometimes omits the status date. Keep the last known one rather
    // than erasing it. Public because the sync also uses it to decide whether
    // a fire changed; keeping the rule here means both uses always agree.
    public DateTime? EffectiveStatusDate(DateTime? reportedStatusDateUtc) =>
        reportedStatusDateUtc ?? StatusDateUtc;
}
