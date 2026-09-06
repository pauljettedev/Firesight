using Firesight.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Firesight.IntegrationTests;

public sealed class PostgisFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgis/postgis:16-3.4")
            .WithDatabase("firesight_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public FiresightDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FiresightDbContext>()
            .UseNpgsql(
                _container.GetConnectionString(),
                npgsql => npgsql.UseNetTopologySuite())
            .Options;

        return new FiresightDbContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgisCollection : ICollectionFixture<PostgisFixture>
{
    public const string Name = "PostGIS";
}
