using FSH.Modules.Parties.Domain.V2.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations.V2;

public sealed class CustomerProfileConfiguration : IEntityTypeConfiguration<CustomerProfile>
{
    public void Configure(EntityTypeBuilder<CustomerProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CustomerProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MaxDiscountPct).HasPrecision(5, 2);
        builder.HasIndex(x => x.PartyId);

        builder.Ignore(x => x.DomainEvents);
    }
}
