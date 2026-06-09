using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class CatalogAliasConfiguration : IEntityTypeConfiguration<CatalogAlias>
{
    public void Configure(EntityTypeBuilder<CatalogAlias> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CatalogAliases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Alias).HasMaxLength(128).IsRequired();
        builder.HasIndex(x => new { x.EntityType, x.TargetId });
        // Trigram index to accelerate fuzzy alias matching.
        builder.HasIndex(x => x.Alias).HasMethod("gin").HasOperators("gin_trgm_ops");
    }
}
