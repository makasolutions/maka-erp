---
paths:
  - "src/**/Persistence/**"
  - "src/**/Entities/**"
---

# ⛔ Superseded — ver `database.md`

Este archivo contenía la plantilla genérica del starter FSH y contradecía las reglas Maka. Lo vigente:

- **No existe `IRepository<T>`** (verificado: 0 usos en `src/`). Prohibido por CLAUDE.md §9.
  En handlers se inyecta el `{Módulo}DbContext` directamente; queries complejas con `Specification<T>`.
- **La BD NO se migra al startup de la API.** Se aplica con el host `FSH.Starter.DbMigrator`
  (`apply` / `seed`). Ver `database.md` y el skill `create-migration`.
- Interfaces reales (`FSH.Framework.Core.Domain`): `IHasTenant { string TenantId }`,
  `IAuditableEntity { CreatedOnUtc, CreatedBy, LastModifiedOnUtc, LastModifiedBy }` (`DateTimeOffset`),
  `ISoftDeletable` (filtro aplicado por el framework en `BaseDbContext` — no escribir
  `HasQueryFilter` manual de soft-delete).
- En subclases de `BaseDbContext`, `base.OnModelCreating(modelBuilder)` se llama **al final**
  (verificado: `CatalogDbContext.cs:59`, `LookupsDbContext.cs:33`, `PartiesDbContext.cs:33`).

Persistencia/EF/migraciones/aislamiento de tenant: **`.agents/rules/database.md`** es la fuente.
