using FSH.Modules.Parties.Domain.V2.Profiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations.V2;

public sealed class ContactProfileConfiguration : IEntityTypeConfiguration<ContactProfile>
{
    public void Configure(EntityTypeBuilder<ContactProfile> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("ContactProfiles");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobTitle).HasMaxLength(128);
        builder.Property(x => x.ContactFunction).HasConversion<short>(); // smallint (SPEC §13)
        builder.HasIndex(x => x.PartyId);

        builder.Ignore(x => x.DomainEvents);
    }
}
