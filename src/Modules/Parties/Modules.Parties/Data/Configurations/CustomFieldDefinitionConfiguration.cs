using System.Text.Json;
using FSH.Modules.Parties.Domain.CustomFields;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FSH.Modules.Parties.Data.Configurations;

public sealed class CustomFieldDefinitionConfiguration : IEntityTypeConfiguration<CustomFieldDefinition>
{
    public void Configure(EntityTypeBuilder<CustomFieldDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ToTable("CustomFieldDefinitions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EntityType).HasConversion<short>();
        builder.Property(x => x.FieldType).HasConversion<short>();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ApiSlug).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Description).HasMaxLength(512);
        builder.Property(x => x.DefaultValue).HasMaxLength(1024);

        // Options → jsonb (patrón del repo: columna jsonb tipada, cf. Billing.OverageRates /
        // Catalog.Product.Specs). ValueConverter System.Text.Json + ValueComparer (EF necesita el
        // comparer para detectar cambios en la colección).
        var optionsConverter = new ValueConverter<IReadOnlyList<CustomFieldOption>, string>(
            v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
            v => string.IsNullOrWhiteSpace(v)
                ? new List<CustomFieldOption>()
                : JsonSerializer.Deserialize<List<CustomFieldOption>>(v, (JsonSerializerOptions?)null) ?? new List<CustomFieldOption>());
        var optionsComparer = new ValueComparer<IReadOnlyList<CustomFieldOption>>(
            (a, b) => (a ?? new List<CustomFieldOption>()).SequenceEqual(b ?? new List<CustomFieldOption>()),
            v => v.Aggregate(0, (acc, o) => HashCode.Combine(acc, o.Value, o.Label, o.Color)),
            v => v.ToList());

        builder.Property(x => x.Options)
            .HasConversion(optionsConverter, optionsComparer)
            .HasColumnType("jsonb")
            .HasColumnName("Options");

        // Índice de apoyo por EntityType (las definiciones se listan por scope). El ÚNICO parcial
        // (TenantId, EntityType, ApiSlug) WHERE Activo se define en PartiesDbContext tras
        // base.OnModelCreating (el shadow TenantId solo existe ahí).
        builder.HasIndex(x => x.EntityType);

        builder.Ignore(x => x.DomainEvents);
    }
}
