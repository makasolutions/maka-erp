---
paths:
  - "src/Modules/**"
---

# ⛔ Superseded — ver `architecture.md` + skill `add-module`

Este archivo describía la estructura vieja del starter genérico (carpetas `Entities/`+`Persistence/`,
`Extensions.cs` con `Add{X}Module()` cableado a mano en Program.cs, `IRepository<T>`, eventos de
integración declarados como `DomainEvent` y publicados directo al bus, permisos
`"modulo.recurso.accion"` en minúsculas). Nada de eso aplica.

Lo vigente:

- Módulo = `IModule` + `[assembly: FshModule(typeof(XModule), order)]` (atributo de **assembly**),
  descubierto por `ModuleLoader` + el footgun de registro en **cuatro** lugares → **`architecture.md`**.
- Slices en `Features/v1/{Area}/{Feature}/`; dominio en `Domain/`; EF en `Data/`.
- Cross-módulo SOLO vía `.Contracts` o integration events **por Outbox** (`eventing.md`) —
  nunca `IEventBus` directo, nunca `DomainEvent` como evento de integración.
- Permisos: `Permissions.{Resource}.{Action}` en `{X}Permissions` del proyecto Contracts
  (verificado: `CatalogPermissions.cs`).

Scaffolding paso a paso: skill **`add-module`**.
