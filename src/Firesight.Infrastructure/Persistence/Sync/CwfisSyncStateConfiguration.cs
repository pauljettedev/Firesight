using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Firesight.Infrastructure.Persistence.Sync;

public sealed class CwfisSyncStateConfiguration : IEntityTypeConfiguration<CwfisSyncState>
{
    public void Configure(EntityTypeBuilder<CwfisSyncState> builder)
    {
        builder.ToTable("CwfisSyncState");

        builder.HasKey(state => state.Source);
        builder.Property(state => state.Source).HasMaxLength(50);
        builder.Property(state => state.LastError).HasMaxLength(2000);
    }
}
