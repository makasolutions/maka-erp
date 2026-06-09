using FSH.Modules.Lookups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Lookups.Data.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(8);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasMany(x => x.Municipalities)
            .WithOne()
            .HasForeignKey(m => m.DepartmentCode)
            .HasPrincipalKey(d => d.Code)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Municipalities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(8);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.DepartmentCode).IsRequired().HasMaxLength(8);
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.DepartmentCode);

        builder.Ignore(x => x.DomainEvents);
    }
}
