using Microsoft.EntityFrameworkCore;

namespace Firesight.Infrastructure.Persistence;

public sealed class DatabaseInitializer(FiresightDbContext dbContext)
{
    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        dbContext.Database.MigrateAsync(cancellationToken);
}
