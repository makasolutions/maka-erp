using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Catalog.Data.Configurations;

public sealed class ScorecardKpiConfiguration : IEntityTypeConfiguration<ScorecardKpi>
{
    public void Configure(EntityTypeBuilder<ScorecardKpi> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ScorecardKpis");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Weight).HasPrecision(9, 2);
        builder.HasIndex(x => x.Code);
    }
}

public sealed class SupplierScorecardConfiguration : IEntityTypeConfiguration<SupplierScorecard>
{
    public void Configure(EntityTypeBuilder<SupplierScorecard> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("SupplierScorecards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PeriodLabel).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Grade).HasConversion<string>().HasMaxLength(2);
        builder.Property(x => x.WeightedScore).HasPrecision(5, 2);
        builder.Property(x => x.Notes).HasMaxLength(2000);

        builder.HasIndex(x => x.SupplierId);
        builder.HasIndex(x => new { x.SupplierId, x.PeriodStart });

        builder.HasMany(x => x.Criteria)
            .WithOne()
            .HasForeignKey(c => c.ScorecardId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(x => x.Criteria).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class ScorecardCriterionConfiguration : IEntityTypeConfiguration<ScorecardCriterion>
{
    public void Configure(EntityTypeBuilder<ScorecardCriterion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ScorecardCriteria");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.KpiCode).HasMaxLength(64).IsRequired();
        builder.Property(x => x.KpiName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Weight).HasPrecision(9, 2);
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.HasIndex(x => x.ScorecardId);
    }
}
