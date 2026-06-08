using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class IndustryConfiguration : IEntityTypeConfiguration<Industry>
{
    public void Configure(EntityTypeBuilder<Industry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Industries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasMany(x => x.Roots).WithOne().HasForeignKey(x => x.IndustryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class IndustryCategoryConfiguration : IEntityTypeConfiguration<IndustryCategory>
{
    public void Configure(EntityTypeBuilder<IndustryCategory> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("IndustryCategories");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.IndustryId, x.RootGoogleCategoryId }).IsUnique();
    }
}

public sealed class TenantIndustryConfiguration : IEntityTypeConfiguration<TenantIndustry>
{
    public void Configure(EntityTypeBuilder<TenantIndustry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("TenantIndustries");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.IndustryId);
    }
}
