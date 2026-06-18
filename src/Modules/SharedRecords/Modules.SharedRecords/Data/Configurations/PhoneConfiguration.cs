using FSH.Modules.SharedRecords.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.SharedRecords.Data.Configurations;

public sealed class PhoneConfiguration : IEntityTypeConfiguration<Phone>
{
    public void Configure(EntityTypeBuilder<Phone> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Phones");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerType).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TypeCode).HasMaxLength(64);
        builder.Property(x => x.Number).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Extension).HasMaxLength(16);
        builder.Property(x => x.CountryCode).HasMaxLength(8);

        // Lookup principal del control: todos los teléfonos de un owner.
        builder.HasIndex(x => new { x.OwnerType, x.OwnerId });

        // El índice único PARCIAL "un solo principal por (TenantId, OwnerType, OwnerId)" se define en
        // SharedRecordsDbContext.OnModelCreating tras base (depende del shadow TenantId).
    }
}
