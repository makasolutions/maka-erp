using FSH.Modules.Parties.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class PartyAddressConfiguration : IEntityTypeConfiguration<PartyAddress>
{
    public void Configure(EntityTypeBuilder<PartyAddress> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyAddresses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.LabelCode).HasMaxLength(64);
        builder.Property(x => x.Country).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Department).HasMaxLength(128);
        builder.Property(x => x.City).HasMaxLength(128);
        builder.Property(x => x.Line).HasMaxLength(256);
        builder.Property(x => x.Barrio).HasMaxLength(128);
        builder.Property(x => x.Reference).HasMaxLength(256);
        builder.Property(x => x.Latitude).HasPrecision(10, 7);
        builder.Property(x => x.Longitude).HasPrecision(10, 7);
        builder.HasIndex(x => x.PartyId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PartyContactConfiguration : IEntityTypeConfiguration<PartyContact>
{
    public void Configure(EntityTypeBuilder<PartyContact> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyContacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reference).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ContactTypeCode).HasMaxLength(64);
        builder.Property(x => x.AreaCode).HasMaxLength(64);
        builder.Property(x => x.IdentificationTypeCode).HasMaxLength(64);
        builder.Property(x => x.IdentificationNumber).HasMaxLength(64);
        builder.Property(x => x.FirstName).HasMaxLength(120);
        builder.Property(x => x.LastName).HasMaxLength(120);
        builder.Property(x => x.FullName).HasMaxLength(240);
        builder.Property(x => x.PositionCode).HasMaxLength(64);
        builder.Property(x => x.ProfessionCode).HasMaxLength(64);
        builder.Property(x => x.GenderCode).HasMaxLength(64);
        builder.Property(x => x.MaritalStatusCode).HasMaxLength(64);
        builder.Property(x => x.Email).HasMaxLength(256);
        builder.Property(x => x.Phone).HasMaxLength(64);
        builder.Property(x => x.Cell).HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(512);
        builder.HasIndex(x => x.PartyId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PartyChannelConfiguration : IEntityTypeConfiguration<PartyChannel>
{
    public void Configure(EntityTypeBuilder<PartyChannel> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyChannels");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ChannelTypeCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Value).IsRequired().HasMaxLength(256);
        builder.Property(x => x.Reference).HasMaxLength(128);
        builder.HasIndex(x => x.PartyId);
        builder.Ignore(x => x.DomainEvents);
    }
}

public sealed class PartyTeamMemberConfiguration : IEntityTypeConfiguration<PartyTeamMember>
{
    public void Configure(EntityTypeBuilder<PartyTeamMember> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyTeamMembers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
        builder.HasIndex(x => x.PartyId);
        builder.HasIndex(x => x.UserId);
        builder.Ignore(x => x.DomainEvents);
    }
}
