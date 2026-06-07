using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class MarketplaceAttributeRequirementConfiguration : IEntityTypeConfiguration<MarketplaceAttributeRequirement>
{
    public void Configure(EntityTypeBuilder<MarketplaceAttributeRequirement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("MarketplaceAttributeRequirements");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Marketplace).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(x => x.CategoryId).IsRequired();
        builder.Property(x => x.AttributeId).IsRequired();
        builder.HasIndex(x => new { x.Marketplace, x.CategoryId, x.AttributeId }).IsUnique();
        builder.HasIndex(x => x.CategoryId);
        builder.Ignore(x => x.DomainEvents);
    }
}
