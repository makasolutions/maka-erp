using FSH.Modules.Lookups.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Lookups.Data.Configurations;

public sealed class BasicTableConfiguration : IEntityTypeConfiguration<BasicTable>
{
    public void Configure(EntityTypeBuilder<BasicTable> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("BasicTables");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TenantId).HasMaxLength(64);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        // Único por (TenantId, Code) sobre filas vivas — la misma tabla global y la de un
        // tenant no chocan porque TenantId difiere; null = global.
        builder.HasIndex(x => new { x.TenantId, x.Code }).IsUnique().HasFilter("\"IsDeleted\" = FALSE");
        builder.HasIndex(x => x.IsDeleted);

        builder.HasMany(x => x.Records)
            .WithOne()
            .HasForeignKey(r => r.BasicTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
