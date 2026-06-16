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
        builder.HasIndex(x => x.PartyId);

        builder.Ignore(x => x.DomainEvents);
    }
}
