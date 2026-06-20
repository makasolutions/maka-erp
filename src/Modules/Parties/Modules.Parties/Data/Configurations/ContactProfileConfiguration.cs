using FSH.Modules.Parties.Domain.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class ContactProfileConfiguration : IEntityTypeConfiguration<ContactProfile>
{
    public void Configure(EntityTypeBuilder<ContactProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ContactProfiles");
        builder.HasKey(x => x.Id);

        // PR-2: faceta adelgazada — solo PartyId (índice ÚNICO vía la 1:1 en PartyConfiguration) +
        // ResponsibleUserId. JobTitle/ContactFunction/IsCommercialContact/IsPrimary migraron a PartyRelationship.

        builder.Ignore(x => x.DomainEvents);
    }
}
