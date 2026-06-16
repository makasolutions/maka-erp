using FSH.Modules.Parties.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CustomerProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaxDiscountPct).HasPrecision(5, 2);
        // Índice ÚNICO sobre PartyId lo crea la relación one-to-one en PartyConfiguration (PR-D2).

        builder.Ignore(x => x.DomainEvents);
    }
}
