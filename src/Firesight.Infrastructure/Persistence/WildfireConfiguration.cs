using Firesight.Domain.Wildfires;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Firesight.Infrastructure.Persistence;

public class WildfireConfiguration : IEntityTypeConfiguration<Wildfire>
{
    public void Configure(EntityTypeBuilder<Wildfire> builder)
    {
        builder.ToTable("Wildfires");

        builder.HasKey(fire => fire.Id);

        builder.Property(fire => fire.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(fire => fire.ExternalId)
            .IsUnique();

        builder.Property(fire => fire.Agency)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fire => fire.Name)
            .HasMaxLength(200);

        builder.Property(fire => fire.Location)
            .IsRequired()
            .HasColumnType("geography (point)");

        builder.Property(fire => fire.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(fire => fire.StatusDateUtc);

        builder.Property(fire => fire.LastSeenInFeedUtc)
            .IsRequired();

        builder.Property(fire => fire.FirstObservedExtinguishedUtc);
    }
}
