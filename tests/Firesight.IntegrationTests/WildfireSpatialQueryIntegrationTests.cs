using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Wildfires;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Firesight.IntegrationTests;

[Collection(PostgisCollection.Name)]
public sealed class WildfireSpatialQueryIntegrationTests(PostgisFixture fixture)
{
    [Fact]
    public async Task FindNearAsync_UsesPostgisDistanceAndOrdersNearestFirst()
    {
        var originId = UniqueId("origin");
        var nearbyId = UniqueId("nearby");
        var distantId = UniqueId("distant");
        var nowUtc = DateTime.UtcNow;

        await using var dbContext = fixture.CreateDbContext();

        dbContext.Wildfires.AddRange(
            CreateFire(originId, 45.4215, -75.6972, nowUtc),
            CreateFire(nearbyId, 45.4765, -75.7013, nowUtc),
            CreateFire(distantId, 43.6532, -79.3832, nowUtc));

        await dbContext.SaveChangesAsync();

        var repository = CreateRepository(dbContext);
        var results = await repository.FindNearAsync(
            latitude: 45.4215,
            longitude: -75.6972,
            radiusKm: 25);

        var matching = results
            .Where(result =>
                result.Wildfire.ExternalId == originId ||
                result.Wildfire.ExternalId == nearbyId ||
                result.Wildfire.ExternalId == distantId)
            .ToList();

        Assert.Equal(2, matching.Count);
        Assert.Equal(originId, matching[0].Wildfire.ExternalId);
        Assert.Equal(nearbyId, matching[1].Wildfire.ExternalId);
        Assert.DoesNotContain(matching, result => result.Wildfire.ExternalId == distantId);
        Assert.Equal(0, matching[0].DistanceKm, 3);
        Assert.InRange(matching[1].DistanceKm, 5, 10);
    }

    private static WildfireRepository CreateRepository(
        Firesight.Infrastructure.Persistence.FiresightDbContext dbContext) =>
        new(
            dbContext,
            Options.Create(new WildfireRetentionOptions { ExtinguishedDays = 7 }),
            Options.Create(new WildfireFreshnessOptions { StaleAfterHours = 48 }));

    private static Wildfire CreateFire(
        string externalId,
        double latitude,
        double longitude,
        DateTime observedUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId,
            Agency = "TEST",
            Location = new Point(longitude, latitude) { SRID = 4326 },
            Status = "OC",
            LastSeenInFeedUtc = observedUtc
        };

    private static string UniqueId(string prefix) =>
        $"integration-spatial-{prefix}-{Guid.NewGuid():N}";
}
