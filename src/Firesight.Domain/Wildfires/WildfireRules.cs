using System.Linq.Expressions;

namespace Firesight.Domain.Wildfires;

// Business rules for which wildfires Firesight returns. Each rule is an
// expression rather than a plain method so EF Core can read it and turn it
// into the SQL WHERE clause. A plain method is a black box to EF Core, which
// would mean loading every fire and filtering in memory.
public static class WildfireRules
{
    public const string ExtinguishedStatus = "EX";

    // Keeps fires that aren't extinguished, or were first seen as extinguished
    // (EX) after the cutoff. This hides fires that have been out for longer
    // than the extinguished retention period. The period is counted from when
    // Firesight first saw the fire as EX, not from the CWFIS status date.
    public static Expression<Func<Wildfire, bool>> IsNotExtinguishedBefore(DateTime cutoffUtc) =>
        fire => fire.Status != ExtinguishedStatus ||
                fire.FirstObservedExtinguishedUtc == null ||
                fire.FirstObservedExtinguishedUtc > cutoffUtc;

    // Keeps fires seen in the feed after the cutoff. This hides fires that
    // have been missing from the feed for longer than the missing retention
    // period. CWFIS often drops a fire when it goes out instead of reporting
    // EX, so without this, those fires would stay on the map forever. Hidden
    // fires aren't deleted, and reappear if CWFIS reports them again.
    public static Expression<Func<Wildfire, bool>> IsSeenInFeedSince(DateTime cutoffUtc) =>
        fire => fire.LastSeenInFeedUtc > cutoffUtc;
}
