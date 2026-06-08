using FSH.Modules.Parties.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Parties");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.IdentificationTypeCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.IdentificationNumber).IsRequired().HasMaxLength(64);
        builder.Property(x => x.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FirstName).HasMaxLength(120);
        builder.Property(x => x.LastName).HasMaxLength(120);
        builder.Property(x => x.ActividadEconomicaCiiuCode).HasMaxLength(64);
        builder.Property(x => x.CreditDaysCode).HasMaxLength(64);
        builder.Property(x => x.TradeName).HasMaxLength(200);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Website).HasMaxLength(256);
        builder.Property(x => x.TaxRegimeCode).HasMaxLength(64);
        builder.Property(x => x.FiscalResponsibilities).HasMaxLength(512);
        builder.Property(x => x.SourceCode).HasMaxLength(64);
        builder.Property(x => x.MarketingType).HasMaxLength(64);
        builder.Property(x => x.GenderCode).HasMaxLength(64);
        builder.Property(x => x.MaritalStatusCode).HasMaxLength(64);
        builder.Property(x => x.CreditCurrency).HasMaxLength(3);
        builder.Property(x => x.CreditLimit).HasPrecision(18, 2);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);

        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Stage).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Roles).HasConversion<int>();

        // El índice ÚNICO por (TenantId, tipo, número) se define en PartiesDbContext
        // tras base.OnModelCreating, porque el shadow TenantId aún no existe aquí.
        builder.HasIndex(x => x.LegalName);
        builder.HasIndex(x => x.Roles);
        builder.HasIndex(x => x.AssignedUserId);
        builder.HasIndex(x => x.IsDeleted);

        builder.HasMany(x => x.Addresses).WithOne().HasForeignKey(a => a.PartyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Contacts).WithOne().HasForeignKey(c => c.PartyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Channels).WithOne().HasForeignKey(c => c.PartyId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Team).WithOne().HasForeignKey(m => m.PartyId).OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(x => x.DomainEvents);
    }
}
