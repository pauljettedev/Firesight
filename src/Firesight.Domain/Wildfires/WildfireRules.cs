using System.Linq.Expressions;

namespace Firesight.Domain.Wildfires;

// Business rules for which wildfires Firesight returns. Each rule is an
// expression rather than a plain method so EF Core can read it and turn it
// into the SQL WHERE clause. A plain method is a black box to EF Core, which
// would mean loading every fire and filtering in memory.
public static class WildfireRules
{
    public const string ExtinguishedStatus = "EX";

    // A fire is kept until it has been extinguished for longer than the
    // retention period. The period is counted from when Firesight first saw it
    // as extinguished, not from the CWFIS status date.
    public static Expression<Func<Wildfire, bool>> IsWithinRetention(DateTime extinguishedCutoffUtc) =>
        fire => fire.Status != ExtinguishedStatus ||
                fire.FirstObservedExtinguishedUtc == null ||
                fire.FirstObservedExtinguishedUtc > extinguishedCutoffUtc;
}
