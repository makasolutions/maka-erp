---
description: Handle EF Core migrations safely. Delegado al skill canónico create-migration. Use when adding entities or changing database schema.
---

# ⛔ Superseded — usar el skill `create-migration`

La receta vigente (build primero por el footgun del snapshot, `--context` + `--output-dir`,
revisión del SQL generado, aplicación vía `DbMigrator -- apply`) es
**`.agents/skills/create-migration/SKILL.md`**. Modelo completo: `.agents/rules/database.md`.
**La API no migra al startup** — siempre el host DbMigrator.

Lo único con valor propio de este archivo, los nombres de contexto (verificados contra
`src/Host/FSH.Starter.Migrations.PostgreSQL/{carpeta}/{X}ModelSnapshot.cs`):

| Carpeta | `--context` |
|---|---|
| Audit | `AuditDbContext` |
| Billing | `BillingDbContext` |
| Catalog | `CatalogDbContext` |
| Chat | `ChatDbContext` |
| Files | `FilesDbContext` |
| Hr | `HrDbContext` |
| Identity | `IdentityDbContext` |
| Lookups | `LookupsDbContext` |
| MultiTenancy | `TenantDbContext` |
| Notifications | `NotificationsDbContext` |
| Parties | `PartiesDbContext` |
| Tickets | `TicketsDbContext` |
| Webhooks | `WebhookDbContext` |
