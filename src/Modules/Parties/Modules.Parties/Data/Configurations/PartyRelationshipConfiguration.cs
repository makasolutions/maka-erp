using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.Relationships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class PartyRelationshipConfiguration : IEntityTypeConfiguration<PartyRelationship>
{
    public void Configure(EntityTypeBuilder<PartyRelationship> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("PartyRelationships");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RelationshipTypeCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.ContactFunctionCode).HasMaxLength(64);
        builder.Property(x => x.JobTitleCode).HasMaxLength(64);
        builder.Property(x => x.CustomFields).HasColumnType("jsonb");

        // Dos FKs a Party (sin navegación). Restrict: una persona o empresa con vínculos NO se puede
        // HARD-deletear (protege el grafo; la baja es lógica vía Party soft-delete / relación inactiva).
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.SourcePartyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Party>().WithMany().HasForeignKey(x => x.TargetPartyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.SourcePartyId);
        builder.HasIndex(x => x.TargetPartyId);
        // El ÚNICO parcial "un principal activo por empresa" se define en PartiesDbContext tras
        // base.OnModelCreating (referencia el shadow TenantId).

        builder.Ignore(x => x.DomainEvents);
    }
}
