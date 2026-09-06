using Firesight.Application.Wildfires;
using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Wildfires;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Firesight.IntegrationTests;

[Collection(PostgisCollection.Name)]
public sealed class WildfireRepositoryIntegrationTests(PostgisFixture fixture)
{
    [Fact]
    public async Task MigrationsApplyAndPostgisIsAvailable()
    {
        await using var dbContext = fixture.CreateDbContext();

        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Empty(pendingMigrations);

        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT PostGIS_Version();";

        await dbContext.Database.OpenConnectionAsync();
        var version = await command.ExecuteScalarAsync();

        Assert.NotNull(version);

        var versionText = version.ToString();
        Assert.False(string.IsNullOrWhiteSpace(versionText));
    }

    [Fact]
    public async Task GeographyPoint_RoundTripsThroughPostgres()
    {
        var externalId = UniqueId("geo");

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Wildfires.Add(new Wildfire
            {
                Id = Guid.NewGuid(),
                ExternalId = externalId,
                Agency = "ON",
                Location = new Point(-75.6972, 45.4215) { SRID = 4326 },
                Status = "OC",
                LastSeenInFeedUtc = DateTime.UtcNow
            });

            await dbContext.SaveChangesAsync();
        }

        await using var verifyContext = fixture.CreateDbContext();
        var saved = await verifyContext.Wildfires
            .SingleAsync(fire => fire.ExternalId == externalId);

        Assert.Equal(-75.6972, saved.Location.X, 4);
        Assert.Equal(45.4215, saved.Location.Y, 4);
        Assert.Equal(4326, saved.Location.SRID);
    }

    [Fact]
    public async Task SynchronizeAsync_ExistingExternalIdChangesRowInsteadOfInsertingDuplicate()
    {
        var externalId = UniqueId("sync");

        await using var dbContext = fixture.CreateDbContext();
        var repository = CreateRepository(dbContext);

        var first = new WildfireImportRecord(
            externalId,
            "ON",
            null,
            46.5,
            -80.5,
            null,
            100,
            "OC",
            new DateTime(2026, 9, 5, 12, 0, 0, DateTimeKind.Utc));

        var second = first with
        {
            AreaHectares = 250,
            Status = "BH",
            StatusDateUtc = new DateTime(2026, 9, 6, 12, 0, 0, DateTimeKind.Utc)
        };

        var firstResult = await repository.SynchronizeAsync([first]);
        var secondResult = await repository.SynchronizeAsync([second]);

        Assert.Equal(1, firstResult.Inserted);
        Assert.Equal(0, firstResult.Changed);

        Assert.Equal(0, secondResult.Inserted);
        Assert.Equal(1, secondResult.Changed);
        Assert.Equal(0, secondResult.Observed);

        var matchingRows = await dbContext.Wildfires
            .Where(fire => fire.ExternalId == externalId)
            .ToListAsync();

        var saved = Assert.Single(matchingRows);
        Assert.Equal(250, saved.AreaHectares);
        Assert.Equal("BH", saved.Status);
        Assert.Equal(second.StatusDateUtc, saved.StatusDateUtc);
    }

    [Fact]
    public async Task GetAllAsync_HidesExtinguishedFireAfterRetentionWindow()
    {
        var expiredId = UniqueId("expired");
        var recentId = UniqueId("recent");
        var now = DateTime.UtcNow;

        await using var dbContext = fixture.CreateDbContext();

        dbContext.Wildfires.AddRange(
            CreateExtinguishedFire(expiredId, now.AddDays(-8)),
            CreateExtinguishedFire(recentId, now.AddDays(-2)));

        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var visible = await repository.GetAllAsync();

        Assert.DoesNotContain(visible, fire => fire.ExternalId == expiredId);
        Assert.Contains(visible, fire => fire.ExternalId == recentId);
    }

    private static WildfireRepository CreateRepository(
        Firesight.Infrastructure.Persistence.FiresightDbContext dbContext) =>
        new(
            dbContext,
            Options.Create(new WildfireRetentionOptions { ExtinguishedDays = 7 }),
            Options.Create(new WildfireFreshnessOptions { StaleAfterHours = 48 }));

    private static Wildfire CreateExtinguishedFire(
        string externalId,
        DateTime firstObservedExtinguishedUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Agency = "ON",
            Location = new Point(-80.5, 46.5) { SRID = 4326 },
            Status = "EX",
            StatusDateUtc = firstObservedExtinguishedUtc,
            LastSeenInFeedUtc = firstObservedExtinguishedUtc,
            FirstObservedExtinguishedUtc = firstObservedExtinguishedUtc
        };

    private static string UniqueId(string prefix) =>
        $"integration-{prefix}-{Guid.NewGuid():N}";
}
