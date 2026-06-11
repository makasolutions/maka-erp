---
description: Crear un módulo nuevo (bounded context). Delegado al skill canónico add-module. Use when adding a new business domain.
---

# ⛔ Superseded — usar el skill `add-module`

Este workflow duplicaba la receta de creación de módulos con patrones del starter genérico
(`Extensions.cs` con `Add{X}Module()`, `IRepository<T>`, carpetas `Entities/`+`Persistence/`)
que contradicen las reglas Maka.

La receta vigente y verificada es **`.agents/skills/add-module/SKILL.md`**: estructura
runtime + Contracts, `[assembly: FshModule(typeof(XModule), order)]`, registro en **cuatro**
lugares (Api + DbMigrator × Mediator + moduleAssemblies), `IDbInitializer`, permisos
`Permissions.{Resource}.{Action}`, carpeta en el proyecto de migraciones, y el slice frontend.

Antes de empezar: `.agents/rules/architecture.md` (límites, orden de módulos, footgun de registro).
