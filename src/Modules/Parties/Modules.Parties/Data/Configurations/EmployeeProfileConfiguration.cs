using FSH.Modules.Parties.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class EmployeeProfileConfiguration : IEntityTypeConfiguration<EmployeeProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("EmployeeProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EmployeeCode).HasMaxLength(64);
        builder.Property(x => x.JobTitle).HasMaxLength(128);
        // Índice ÚNICO sobre PartyId lo crea la relación one-to-one en PartyConfiguration (PR-D2).

        builder.Ignore(x => x.DomainEvents);
    }
}
