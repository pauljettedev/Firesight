using Firesight.Application.Wildfires;
using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Persistence;
using Firesight.Infrastructure.Wildfires;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Firesight.UnitTests;

public sealed class WildfireRepositoryChangeDetectionTests
{
    [Fact]
    public async Task SynchronizeAsync_UnchangedExistingFire_IsObservedNotChanged()
    {
        await using var dbContext = CreateDbContext();
        var existing = CreateExistingFire();
        dbContext.Wildfires.Add(existing);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var previousLastSeen = existing.LastSeenInFeedUtc;

        var result = await repository.SynchronizeAsync([CreateMatchingImport()]);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(0, result.Changed);
        Assert.Equal(1, result.Observed);
        Assert.True(existing.LastSeenInFeedUtc >= previousLastSeen);
    }

    [Fact]
    public async Task SynchronizeAsync_SourceFieldChanged_CountsAsChanged()
    {
        await using var dbContext = CreateDbContext();
        var existing = CreateExistingFire();
        dbContext.Wildfires.Add(existing);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var incoming = CreateMatchingImport() with { AreaHectares = 2500 };

        var result = await repository.SynchronizeAsync([incoming]);

        Assert.Equal(0, result.Inserted);
        Assert.Equal(1, result.Changed);
        Assert.Equal(0, result.Observed);
        Assert.Equal(2500, existing.AreaHectares);
    }

    [Fact]
    public async Task SynchronizeAsync_StatusDateChange_CountsAsChanged()
    {
        await using var dbContext = CreateDbContext();
        var existing = CreateExistingFire();
        dbContext.Wildfires.Add(existing);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var incoming = CreateMatchingImport() with
        {
            StatusDateUtc = existing.StatusDateUtc!.Value.AddHours(2)
        };

        var result = await repository.SynchronizeAsync([incoming]);

        Assert.Equal(1, result.Changed);
        Assert.Equal(0, result.Observed);
    }

    [Fact]
    public async Task SynchronizeAsync_MissingIncomingStatusDate_DoesNotEraseOrCountChange()
    {
        await using var dbContext = CreateDbContext();
        var existing = CreateExistingFire();
        var originalStatusDate = existing.StatusDateUtc;
        dbContext.Wildfires.Add(existing);
        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var incoming = CreateMatchingImport() with { StatusDateUtc = null };

        var result = await repository.SynchronizeAsync([incoming]);

        Assert.Equal(0, result.Changed);
        Assert.Equal(1, result.Observed);
        Assert.Equal(originalStatusDate, existing.StatusDateUtc);
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
            Options.Create(new WildfireRetentionOptions()),
            Options.Create(new WildfireFreshnessOptions()));

    private static Wildfire CreateExistingFire() =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = "2026_ON_TEST_001",
            Agency = "ON",
            Name = null,
            Location = new Point(-80.5, 46.5) { SRID = 4326 },
            StartDate = null,
            AreaHectares = 1250,
            Status = "OC",
            StatusDateUtc = new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc),
            LastSeenInFeedUtc = new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc)
        };

    private static WildfireImportRecord CreateMatchingImport() =>
        new(
            "2026_ON_TEST_001",
            "ON",
            null,
            46.5,
            -80.5,
            null,
            1250,
            "OC",
            new DateTime(2026, 9, 4, 12, 0, 0, DateTimeKind.Utc));
}
