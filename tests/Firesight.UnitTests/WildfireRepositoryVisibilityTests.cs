using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Persistence;
using Firesight.Infrastructure.Persistence.Sync;
using Firesight.Infrastructure.Wildfires;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Firesight.UnitTests;

// Tests how the repository combines the two hiding rules: fires extinguished
// for 7 days, and fires missing from the feed for 5 days. Uses EF Core's
// in-memory database, so no PostGIS is needed.
public sealed class WildfireRepositoryVisibilityTests
{
    [Fact]
    public async Task GetAllAsync_NoSuccessfulSyncYet_ShowsFireNotSeenFor30Days()
    {
        // With no successful sync, no fire can be missing from the feed.
        await using var dbContext = CreateDbContext();
        var fire = CreateFire("OC", lastSeenInFeedUtc: DateTime.UtcNow.AddDays(-30));
        dbContext.Wildfires.Add(fire);
        await dbContext.SaveChangesAsync();

        var visible = await CreateRepository(dbContext).GetAllAsync();

        Assert.Single(visible);
    }

    [Fact]
    public async Task GetAllAsync_RecentlyExtinguishedButMissingFromFeed_IsHidden()
    {
        // Extinguished only 3 days ago, so the extinguished rule alone would
        // show it. It has been missing from the feed for 6 days, so it's hidden.
        var now = DateTime.UtcNow;

        await using var dbContext = CreateDbContext();
        var fire = CreateFire("EX", lastSeenInFeedUtc: now.AddDays(-6));
        fire.FirstObservedExtinguishedUtc = now.AddDays(-3);
        dbContext.Wildfires.Add(fire);
        dbContext.CwfisSyncStates.Add(new CwfisSyncState { LastSuccessfulFetchUtc = now });
        await dbContext.SaveChangesAsync();

        var visible = await CreateRepository(dbContext).GetAllAsync();

        Assert.Empty(visible);
    }

    [Fact]
    public async Task GetAllAsync_ExtinguishedLongAgoButStillInFeed_IsHidden()
    {
        // Still in the feed, so the missing rule alone would show it.
        // It has been extinguished for 10 days, so it's hidden.
        var now = DateTime.UtcNow;

        await using var dbContext = CreateDbContext();
        var fire = CreateFire("EX", lastSeenInFeedUtc: now);
        fire.FirstObservedExtinguishedUtc = now.AddDays(-10);
        dbContext.Wildfires.Add(fire);
        dbContext.CwfisSyncStates.Add(new CwfisSyncState { LastSuccessfulFetchUtc = now });
        await dbContext.SaveChangesAsync();

        var visible = await CreateRepository(dbContext).GetAllAsync();

        Assert.Empty(visible);
    }

    private static FiresightDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FiresightDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FiresightDbContext(options);
    }

    private static WildfireRepository CreateRepository(FiresightDbContext dbContext) =>
        new(
            dbContext,
            Options.Create(new WildfireRetentionOptions
            {
                ExtinguishedDays = 7,
                MissingFromFeedDays = 5
            }),
            Options.Create(new WildfireFreshnessOptions { StaleAfterHours = 48 }));

    private static Wildfire CreateFire(string status, DateTime lastSeenInFeedUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = $"cwfis:test-{Guid.NewGuid():N}",
            Agency = "NT",
            Location = new Point(-117.5, 62.5) { SRID = 4326 },
            Status = status,
            LastSeenInFeedUtc = lastSeenInFeedUtc
        };
}
