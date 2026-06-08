using FSH.Modules.Hr.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Hr.Data.Configurations;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("Employees");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.PartyId).IsUnique();

        builder.Property(x => x.MainCurrency).HasMaxLength(3);
        builder.Property(x => x.BranchCode).HasMaxLength(64);
        builder.Property(x => x.CostCenterCode).HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.HealthProviderCode).HasMaxLength(64);
        builder.Property(x => x.PensionFundCode).HasMaxLength(64);
        builder.Property(x => x.SeveranceFundCode).HasMaxLength(64);
        builder.Property(x => x.CcfCode).HasMaxLength(64);
        builder.Property(x => x.ArlProviderCode).HasMaxLength(64);
        builder.Property(x => x.PaymentMethodCode).HasMaxLength(64);
        builder.Property(x => x.LaborDepartmentCode).HasMaxLength(64);
        builder.Property(x => x.PositionCode).HasMaxLength(64);
        builder.Property(x => x.ContractTypeCode).HasMaxLength(64);
        builder.Property(x => x.ContractDurationCode).HasMaxLength(64);
        builder.Property(x => x.ArlRiskLevelCode).HasMaxLength(64);
        builder.Property(x => x.RestDays).HasMaxLength(64);
        builder.Property(x => x.SalaryTypeCode).HasMaxLength(64);
        builder.Property(x => x.BaseSalary).HasPrecision(18, 2);
        builder.Property(x => x.DeletedBy).HasMaxLength(64);
    }
}
