using FSH.Modules.Parties.Domain.V2.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations.V2;

public sealed class PartnerProfileConfiguration : IEntityTypeConfiguration<PartnerProfile>
{
    public void Configure(EntityTypeBuilder<PartnerProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartnerProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SharePercentage).HasPrecision(5, 2);
        builder.Property(x => x.Status).HasMaxLength(64);
        // Índice ÚNICO sobre PartyId lo crea la relación one-to-one en PartyConfiguration (PR-D2).

        builder.Ignore(x => x.DomainEvents);
    }
}
