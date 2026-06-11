---
name: add-entity
description: Create a domain entity with multi-tenancy, auditing, soft-delete, and domain events. Use when adding new database entities to a module.
argument-hint: [ModuleName] [EntityName]
---

# Add Entity

Crear una entidad de dominio con los patrones reales del repo. Interfaces verificadas en
`src/BuildingBlocks/Core/Domain/`. Antes de empezar: `.agents/rules/database.md`.

## Entity Template

```csharp
using FSH.Framework.Core.Domain;

namespace FSH.Modules.{Module}.Domain;

public sealed class {Entity} : AggregateRoot<Guid>, ISoftDeletable
{
    // Propiedades de dominio — private set, mutación solo por métodos
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public string? Description { get; private set; }

    // ISoftDeletable — el FILTRO lo aplica el framework (BaseDbContext), no lo escribas tú
    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    private {Entity}() { }   // EF Core

    public static {Entity} Create(string name, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        var entity = new {Entity}
        {
            Id = Guid.CreateVersion7(),   // Ids de ENTIDAD: V7 (orden temporal). NewGuid solo para Ids de evento.
            Name = name.Trim(),
            Price = price,
        };
        entity.AddDomainEvent(new {Entity}CreatedEvent(entity.Id));
        return entity;
    }

    public void UpdateDetails(string name, decimal price, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name.Trim();
        Price = price;
        Description = description?.Trim();
        AddDomainEvent(new {Entity}UpdatedEvent(Id));
    }
}
```

### Tenant y auditoría — los estampa el framework

- **No declares `TenantId` ni lo pases en `Create(...)`**: `BaseEntity`/`BaseDbContext` manejan el
  tenant (filtro default-ON). Opt-out global solo vía `IGlobalEntity` (`database.md`).
- Auditoría (`IAuditableEntity`): `CreatedOnUtc` / `CreatedBy` / `LastModifiedOnUtc` /
  `LastModifiedBy` (`DateTimeOffset`) — estampados por interceptor. **No** uses `CreatedAt`/
  `LastModifiedAt`: esas firmas no existen.

## Domain Events

```csharp
public sealed record {Entity}CreatedEvent(Guid {Entity}Id) : DomainEvent;
public sealed record {Entity}UpdatedEvent(Guid {Entity}Id) : DomainEvent;
```

(Eventos **de integración** cross-módulo son otra cosa: `IIntegrationEvent` + Outbox — skill
`add-integration-event`.)

## EF Core Configuration

```csharp
public sealed class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
{
    public void Configure(EntityTypeBuilder<{Entity}> builder)
    {
        builder.ToTable("{Entities}");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Price).HasPrecision(18, 2);
        builder.HasIndex(x => x.Name);
    }
}
```

- **No escribas `HasQueryFilter` de soft-delete ni de tenant** — los aplica el framework
  (`ModelBuilderExtensions`, filtro nombrado). Escribirlo a mano lo duplica o lo pisa.
- Hijo alcanzado SOLO por colección de navegación del padre →
  `Property(x => x.Id).ValueGeneratedNever()` (gotcha de `database.md`).

## Register in DbContext

```csharp
public sealed class {Module}DbContext(...) : BaseDbContext(...)
{
    public DbSet<{Entity}> {Entities} => Set<{Entity}>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("{module}");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof({Module}DbContext).Assembly);
        base.OnModelCreating(modelBuilder);   // AL FINAL — aplica filtros tenant + soft-delete
    }
}
```

(Verificado: `CatalogDbContext.cs:59`, `LookupsDbContext.cs:33` — `base.OnModelCreating` **siempre al final**.)

## Migración

Seguir el skill **`create-migration`** (build primero, `--context {Module}DbContext`,
`--output-dir {Module}`, aplicar con `DbMigrator -- apply`). **La API no migra al startup.**

## Checklist

- [ ] `AggregateRoot<Guid>` (+ `ISoftDeletable` si aplica; `IGlobalEntity` SOLO si debe ser cross-tenant)
- [ ] Constructor privado + factory `Create(...)` con `Guid.CreateVersion7()`
- [ ] Sin `TenantId` manual, sin `HasQueryFilter` manual
- [ ] Domain events en cambios de estado significativos
- [ ] EF config + DbSet + `base.OnModelCreating` al final
- [ ] Migración vía `create-migration` + DbMigrator
