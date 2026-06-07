using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class CategoryAttributeConfiguration : IEntityTypeConfiguration<CategoryAttribute>
{
    public void Configure(EntityTypeBuilder<CategoryAttribute> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CategoryAttributes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CategoryId).IsRequired();
        builder.Property(x => x.AttributeId).IsRequired();
        builder.HasIndex(x => new { x.CategoryId, x.AttributeId }).IsUnique();
        builder.HasIndex(x => x.AttributeId);
        builder.Ignore(x => x.DomainEvents);
    }
}
