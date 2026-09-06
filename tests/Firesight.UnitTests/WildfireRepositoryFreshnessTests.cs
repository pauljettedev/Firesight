using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Persistence;
using Firesight.Infrastructure.Wildfires;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace Firesight.UnitTests;

public sealed class WildfireRepositoryFreshnessTests
{
    [Theory]
    [InlineData("OC", 12, false)]
    [InlineData("OC", 60, true)]
    [InlineData("EX", 12, false)]
    [InlineData("EX", 60, true)]
    public async Task GetAllAsync_DerivesStalenessFromLastSeenForAnyCwfisStatus(
        string status,
        int hoursSinceLastSeen,
        bool expectedIsStale)
    {
        var options = new DbContextOptionsBuilder<FiresightDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var dbContext = new FiresightDbContext(options);

        var lastSeenUtc = DateTime.UtcNow.AddHours(-hoursSinceLastSeen);

        dbContext.Wildfires.Add(new Wildfire
        {
            Id = Guid.NewGuid(),
            ExternalId = $"cwfis:test-{status}-{hoursSinceLastSeen}",
            Agency = "ON",
            Name = "Test Fire",
            Location = new Point(-80.5, 46.5) { SRID = 4326 },
            Status = status,
            LastSeenInFeedUtc = lastSeenUtc,
            FirstObservedExtinguishedUtc =
                status == "EX" ? lastSeenUtc : null
        });

        await dbContext.SaveChangesAsync();

        var repository = new WildfireRepository(
            dbContext,
            Options.Create(new WildfireRetentionOptions { ExtinguishedDays = 7 }),
            Options.Create(new WildfireFreshnessOptions { StaleAfterHours = 48 }));

        var result = await repository.GetAllAsync();

        var wildfire = Assert.Single(result);
        Assert.Equal(expectedIsStale, wildfire.IsStale);
    }
}
