using Firesight.Domain.Wildfires;
using Firesight.Infrastructure.Persistence.Sync;
using Microsoft.EntityFrameworkCore;

namespace Firesight.Infrastructure.Persistence;

public class FiresightDbContext(DbContextOptions<FiresightDbContext> options)
    : DbContext(options)
{
    public DbSet<Wildfire> Wildfires => Set<Wildfire>();
    public DbSet<CwfisSyncState> CwfisSyncStates => Set<CwfisSyncState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FiresightDbContext).Assembly);
    }
}
