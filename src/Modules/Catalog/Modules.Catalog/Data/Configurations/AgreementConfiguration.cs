using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class AgreementConfiguration : IEntityTypeConfiguration<Agreement>
{
    public void Configure(EntityTypeBuilder<Agreement> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Agreements");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AgreementType).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.DispatchResponsible).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.WaybillResponsible).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.SettlementResponsible).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.FailedDeliveryPolicy).HasMaxLength(1000);
        builder.Property(x => x.ReturnsPolicy).HasMaxLength(1000);
        builder.Property(x => x.WarrantyPolicy).HasMaxLength(1000);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Rules)
            .WithOne()
            .HasForeignKey(r => r.AgreementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Rules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class AgreementRuleConfiguration : IEntityTypeConfiguration<AgreementRule>
{
    public void Configure(EntityTypeBuilder<AgreementRule> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("AgreementRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RuleType).HasConversion<string>().HasMaxLength(64);
        builder.Property(x => x.NumericValue).HasPrecision(18, 4);
        builder.Property(x => x.TextValue).HasMaxLength(256);
        builder.HasIndex(x => x.AgreementId);
    }
}
