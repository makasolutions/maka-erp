using FSH.Modules.SharedRecords.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.SharedRecords.Data.Configurations;

public sealed class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Addresses");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.LabelCode).HasMaxLength(64);
        builder.Property(x => x.Country).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Department).HasMaxLength(128);
        builder.Property(x => x.City).HasMaxLength(128);
        builder.Property(x => x.DepartmentCode).HasMaxLength(8);
        builder.Property(x => x.MunicipalityCode).HasMaxLength(8);
        builder.Property(x => x.Line).HasMaxLength(256);
        builder.Property(x => x.Barrio).HasMaxLength(128);
        builder.Property(x => x.Reference).HasMaxLength(256);
        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);

        // Lookup principal del control: todas las direcciones de un owner.
        builder.HasIndex(x => new { x.OwnerType, x.OwnerId });

        // El índice único PARCIAL "una principal por (TenantId, OwnerType, OwnerId)" se define en
        // SharedRecordsDbContext.OnModelCreating tras base (depende del shadow TenantId).
    }
}
