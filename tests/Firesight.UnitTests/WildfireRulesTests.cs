using Firesight.Domain.Wildfires;
using NetTopologySuite.Geometries;

namespace Firesight.UnitTests;

// Tests the wildfire rules directly, with no database. IsWithinRetention is run
// through an in-memory list with AsQueryable(), which uses the same expression
// the repository hands to EF Core.
public sealed class WildfireRulesTests
{
    private static readonly DateTime NowUtc = new(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("OC", null, true)]
    [InlineData("UC", 30, true)]
    [InlineData("EX", null, true)]
    [InlineData("EX", 3, true)]
    [InlineData("EX", 7, false)]
    [InlineData("EX", 10, false)]
    public void IsWithinRetention_DropsFiresExtinguishedForRetentionPeriodOrLonger(
        string status,
        int? daysSinceFirstExtinguished,
        bool expectedKept)
    {
        var fire = CreateFire(status);
        if (daysSinceFirstExtinguished is not null)
        {
            fire.FirstObservedExtinguishedUtc = NowUtc.AddDays(-daysSinceFirstExtinguished.Value);
        }

        var extinguishedCutoffUtc = NowUtc.AddDays(-7);

        var kept = new[] { fire }
            .AsQueryable()
            .Where(WildfireRules.IsWithinRetention(extinguishedCutoffUtc))
            .ToList();

        Assert.Equal(expectedKept, kept.Any());
    }

    [Theory]
    [InlineData(12, false)]
    [InlineData(48, false)]
    [InlineData(60, true)]
    public void IsStale_IsTrueOnlyWhenLastSeenBeforeCutoff(
        int hoursSinceLastSeen,
        bool expectedStale)
    {
        var fire = CreateFire("OC");
        fire.LastSeenInFeedUtc = NowUtc.AddHours(-hoursSinceLastSeen);

        var staleCutoffUtc = NowUtc.AddHours(-48);

        Assert.Equal(expectedStale, fire.IsStale(staleCutoffUtc));
    }

    [Fact]
    public void RecordStatus_FirstExtinguished_SetsFirstObservedTime()
    {
        var fire = CreateFire("UC");

        fire.RecordStatus("EX", NowUtc, NowUtc);

        Assert.Equal("EX", fire.Status);
        Assert.Equal(NowUtc, fire.FirstObservedExtinguishedUtc);
    }

    [Fact]
    public void RecordStatus_StillExtinguished_KeepsFirstObservedTime()
    {
        var fire = CreateFire("EX");
        var firstSeenUtc = NowUtc.AddDays(-3);
        fire.FirstObservedExtinguishedUtc = firstSeenUtc;

        fire.RecordStatus("EX", NowUtc, NowUtc);

        Assert.Equal(firstSeenUtc, fire.FirstObservedExtinguishedUtc);
    }

    [Fact]
    public void RecordStatus_NoLongerExtinguished_ClearsFirstObservedTime()
    {
        var fire = CreateFire("EX");
        fire.FirstObservedExtinguishedUtc = NowUtc.AddDays(-3);

        fire.RecordStatus("OC", NowUtc, NowUtc);

        Assert.Equal("OC", fire.Status);
        Assert.Null(fire.FirstObservedExtinguishedUtc);
    }

    [Fact]
    public void RecordStatus_LowercaseEx_IsNotTreatedAsExtinguished()
    {
        // Must match IsWithinRetention, which compares exactly in SQL.
        var fire = CreateFire("UC");

        fire.RecordStatus("ex", NowUtc, NowUtc);

        Assert.Null(fire.FirstObservedExtinguishedUtc);
    }

    [Fact]
    public void RecordStatus_MissingStatusDate_KeepsLastKnownDate()
    {
        var fire = CreateFire("OC");
        var knownStatusDateUtc = NowUtc.AddDays(-1);
        fire.StatusDateUtc = knownStatusDateUtc;

        fire.RecordStatus("OC", null, NowUtc);

        Assert.Equal(knownStatusDateUtc, fire.StatusDateUtc);
    }

    [Fact]
    public void RecordStatus_UpdatesLastSeenInFeed()
    {
        var fire = CreateFire("OC");
        fire.LastSeenInFeedUtc = NowUtc.AddDays(-1);

        fire.RecordStatus("OC", null, NowUtc);

        Assert.Equal(NowUtc, fire.LastSeenInFeedUtc);
    }

    private static Wildfire CreateFire(string status) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = "cwfis:test",
            Agency = "ON",
            Location = new Point(-80.5, 46.5) { SRID = 4326 },
            Status = status,
            LastSeenInFeedUtc = NowUtc
        };
}
