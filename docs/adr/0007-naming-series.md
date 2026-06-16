# ADR-0007 · NamingSeries — numeración de documentos con resolución DIAN

- **Estado:** Aceptado · junio 2026
- **Decisores:** Juan (CEO/sistemas) + asistente de planeación
- **Ligado a:** ADR-0006 (`ISubmittable`/`DocStatus` — el patrón canónico de documento submittable que consumirá `NamingSeries`), CLAUDE.md §RF-BILL-5 (*"Resoluciones: rangos de numeración, vigencias, alerta al 80% de uso del rango"*).
- **Evidencia:** `src/Modules/NamingSeries/`, `src/Tests/NamingSeries.Tests/` (21/21 unit tests verdes).

---

## Contexto

Múltiples módulos transaccionales necesitan emitir documentos con números **consecutivos, únicos y trazables** dentro de un rango autorizado: facturas DIAN, notas crédito/débito, órdenes de compra, asientos contables, traslados, conteos físicos. La regla DIAN colombiana es estricta: cada resolución fija un rango `[from, to]`, una vigencia, y exige una **secuencia sin huecos** dentro del rango.

Hoy, `BillingService.BuildInvoiceNumber` formatea un número compuesto en código C# sin tabla, sin atomicidad transaccional, sin awareness DIAN — es deuda obvia que sirvió en un módulo SaaS interno pero no escala al ERP fiscal. Cada futuro módulo transaccional necesitaría su propio numerador, multiplicando el riesgo.

Frappe Framework (ERPNext) resuelve esto con un building block "Naming Series" reutilizable. Tomamos el concepto y lo adaptamos a .NET fuerte tipado + DIAN.

## Decisión

Crear un **módulo propio** `src/Modules/NamingSeries/` con:

### Dominio

1. **`NamingPattern`** — value object inmutable con AST del patrón. Tokens soportados:
   - `{YYYY}` (año 4 dígitos), `{YY}` (año 2 dígitos)
   - `{MM}` (mes), `{DD}` (día)
   - `{#}+` (contador, N `#` = N dígitos de padding cero)
   - Literal: cualquier carácter ASCII fuera de `{}`

   Reglas duras: exactamente UN bloque `{#}+`, sin tokens desconocidos. Si excede padding, mantiene longitud real (DIAN no exige padding fijo).

2. **`NamingSeries`** — agregado raíz tenant-aislado con `Allocate(now)`:
   - Valida `ValidFrom/ValidUntil`, rango (`CurrentValue + 1 <= To`), serie abierta.
   - Incrementa contador.
   - Si cruza umbral 80% por primera vez: emite `NamingSeriesThresholdReachedDomainEvent` (idempotente — flag `Notified80Pct`).
   - Retorna número formateado.
   - Lanza excepción específica (`NamingSeriesExhausted`, `Expired`, `NotYetValid`, `Closed`) ante violación.

3. **`NamingSeriesAllocation`** — log inmutable con `(NamingSeriesId, Number, DocumentId?, DocumentType, AllocatedAtUtc, AllocatedBy)`. Auditoría DIAN: qué documento consumió cada número.

4. **DIAN fields opcionales en el agregado**: `ResolutionNumber`, `ResolutionDate`, `ResolutionTechnicalKey` (nullable; obligatorios para `DocumentType` fiscal, validado a nivel de aplicación).

### Contracts

- **`INamingSeriesAllocator.AllocateAsync(documentType, documentId, DbConnection, DbTransaction, ct)`** — superficie pública cross-módulo. Recibe la conexión + transacción del caller (módulo cliente) para garantizar atomicidad: si la tx del documento hace rollback, el incremento del contador se revierte automáticamente.
- Integration events: `NamingSeriesThresholdReachedIntegrationEvent`, `NamingSeriesExhaustedIntegrationEvent`.
- DTOs: `NamingSeriesDto`, `NamingSeriesUsageReportRow`.

### Permisos

`NamingSeries.View` (IsBasic) · `NamingSeries.Create` · `NamingSeries.Close`. Cerrar una resolución DIAN es decisión humana sensible — separado de simple consulta.

### Generador atómico

**SELECT FOR UPDATE** sobre la fila de la serie activa, dentro de la transacción del caller. Validaciones (vigencia, rango, ambigüedad) inline en la misma tx. No usar Postgres SEQUENCE: rompe el modelo de rango bounded DIAN y obliga DDL en cada cambio de resolución.

### Comportamiento al agotarse

Excepción + bloqueo del `Submit` del documento. El sistema **NUNCA inventa números fuera del rango autorizado**. Renovación = decisión humana (nueva resolución DIAN, registrada como nueva fila).

## Alcance del PR1 (este PR)

- Módulo + Contracts + tests unitarios + permisos + DbContext + Configurations + DbInitializer + DbContextFactory.
- Wiring 4-place (Api/DbMigrator/Mediator/`moduleAssemblies` + `wolverineSchemas`).
- ADR-0007 cerrado como Aceptado.

**Diferido a PR2** (Features CRUD + adopción en Billing):
- Migración EF `NamingSeries_Initial` (ver §Deuda conocida).
- `NamingSeriesAllocator` (SQL crudo SELECT FOR UPDATE) — pendiente decisión del usuario sobre cómo publicar `ThresholdReached` desde dentro de la operación que usa la conexión del caller.
- Features v1: Create/Get/Close/UsageReport.
- Integration tests: concurrencia 50 tasks, rollback, tenant isolation, cross-DbContext tx.
- Adopción en `BillingService` como primer cliente real.

## Validación upstream (FSH / Mukesh)

FullStackHero no tiene patrón equivalente — es agnóstico al dominio fiscal. La decisión es **divergencia consciente**: tomamos como referente conceptual Frappe Naming Series (Python, ERPNext) y la adaptamos a .NET fuerte tipado + DIAN Colombia (rango bounded obligatorio, atomicidad transaccional cross-module via SQL crudo sobre la conexión del caller). No hay opción upstream que evaluar.

## Entidades futuras que consumirán `INamingSeriesAllocator`

| Módulo | Entidad |
|---|---|
| **DIAN (M8)** | `SalesInvoice`, `CreditNote`, `DebitNote`, `EquivalentDocument`, `PayrollDocument` |
| **Accounting (M7)** | `JournalEntry`, `BankReconciliation` |
| **Inventory (M3)** | `StockAdjustment`, `CycleCount`, `Transfer` |
| **Purchasing (M4)** | `PurchaseRequest`, `PurchaseOrder`, `GoodsReceipt` |
| **Imports (M4)** | `ImportOrder` |
| **Orders (M2)** | `Quotation`, `SalesOrder`, `DeliveryNote`, `ReturnOrder` |
| **Warranties (M10)** | `WarrantyCase` |
| **Billing (SaaS interno)** | adopción del `BuildInvoiceNumber` actual como primer cliente real en PR2 |

Todas implementan `ISubmittable` (ADR-0006): el número se asigna en `Submit()` desde el command handler, en la misma transacción que firma el documento. El draft NO consume número del rango (regla DIAN: sin huecos).

## Consecuencias

**Estructurales:**
- Building block transversal queryable y administrable que codifica una regla legal Colombia.
- `NamingSeriesDbContext` propio con schema `naming` + tabla de allocations para auditoría.
- Linaje `(Document → Number → Series → Resolution)` queryable de manera uniforme — base para reportes DIAN.

**Operativas:**
- CFO/Admin gestiona resoluciones DIAN desde una UI única (Configuración → Resoluciones DIAN — llega en PR3).
- Alerta automática al 80% del rango via integration event → Notifications.

**De documentación:**
- Cuando un módulo nuevo (DIAN, Accounting, Purchasing, etc.) cree su primera entidad submittable: debe inyectar `INamingSeriesAllocator` y llamarlo desde el command handler de Submit, NO desde la entidad. La entidad solo recibe el número ya asignado vía parámetro de su método `Submit(userId, allocatedNumber)`.

## Deuda conocida

### Migración EF diferida a PR2

`dotnet ef dbcontext list` no descubre `NamingSeriesDbContext` aunque:

- `ProjectReference` existe en `FSH.Starter.Api.csproj` y `FSH.Starter.Migrations.PostgreSQL.csproj`.
- `NamingSeriesDbContextFactory` implementa el patrón idéntico a los otros 5 módulos (`ChatDbContextFactory`, `FilesDbContextFactory`, `IdentityDbContextFactory`, `NotificationsDbContextFactory`, `WebhookDbContextFactory`) que **sí** aparecen en el discovery tras el commit `04a0a840 refactor(ef): IDesignTimeDbContextFactory en modulos Wolverine-enrolados`.
- El tipo `NamingSeriesDbContextFactory` está físicamente en el .dll (`grep -ao "NamingSeriesDbContextFactory" FSH.Modules.NamingSeries.dll` confirma).
- La hipótesis de **colisión de naming** (`Domain.NamingSeries` vs `NamingSeriesDbContext` vs namespace `FSH.Modules.NamingSeries`) fue descartada empíricamente: renombrar el DbContext a `NumberingDbContext` no cambió el resultado.

**Hipótesis pendiente de confirmar (al cierre de PR1):** EF Core puede requerir al menos una migración existente para incluir un DbContext en el discovery.

### Actualización (PR-B de Parties, jun 2026) — CAUSA PRIMARIA CONFIRMADA

El experimento de PR-B de Parties aisló la causa primaria, distinta de lo que se creía:

**Causa primaria (CONFIRMADA): la integración Wolverine EF envenena la enumeración design-time, y rompe `dotnet ef` para cualquier contexto SIN factory propia.**

Evidencia dura: `PartiesDbContext` (1) usa `AddHeroDbContext` estándar — **NO** Wolverine, (2) tiene **5 migraciones previas**, (3) ya aparecía en `dotnet ef dbcontext list`. Aun así, `dotnet ef migrations add --context PartiesDbContext` **falló con el MISMO error** `Cannot resolve scoped ISaveChangesInterceptor from root provider`. Apenas se le añadió una `PartiesDbContextFactory` (patrón idéntico a las 5 factories de los módulos Wolverine), la migración **funcionó al primer intento** (`Done.`, 9 tablas, diff verificado).

Mecánica: cuando se targetea un contexto **con** factory + `--context`, EF usa la factory directamente y **cortocircuita** la enumeración del host-provider. Sin factory, EF construye el host-provider y enumera todos los DbContexts; los contextos enrolados con `AddDbContextWithWolverineIntegration` resuelven `IEnumerable<ISaveChangesInterceptor>` (scoped) desde el root provider durante ese callback y lanzan, **abortando la operación completa** sin importar el `--context`.

→ Esto **descarta** la hipótesis "necesita migración previa" como causa primaria (Parties tenía 5 y falló igual). **El fix general es: cada contexto que necesite migraciones debe tener su `IDesignTimeDbContextFactory`.**

**Residual de NamingSeries (NO resuelto, dos candidatas — no sobre-concluir):**
`NamingSeriesDbContext` **tiene** factory y aun así falló en su PR. Comparando:
- `IdentityDbContext`: Wolverine + factory + **con** migraciones previas → `dbcontext info` funciona.
- `PartiesDbContext`: no-Wolverine + factory + **con** migraciones previas → funciona (PR-B).
- `NamingSeriesDbContext`: Wolverine + factory + **SIN** migraciones previas → falla.

El residual de NamingSeries correlaciona con **"sin migración previa"**, pero su perfil (Wolverine **y** sin-migración) no aísla cuál de las dos lo bloquea. Quedan **dos causas candidatas** para el residual, sin distinguir aún:
1. **Wolverine-específica**: el callback de `AddDbContextWithWolverineIntegration` resolviendo scoped services aun por la ruta de factory.
2. **Falta de migración previa**: EF no añade una "primera" migración a un contexto que nunca tuvo ninguna, incluso con factory.

Para aislarlas haría falta el caso "no-Wolverine + factory + sin migración previa" — que no existe en el repo. Fix de NamingSeries (su PR2): probar primero replicar el patrón Parties (ya tiene factory); si persiste, generar su 1ª migración por workaround (handcraft de Migration + Designer + ModelSnapshot, o `dotnet ef migrations script`). **El misterio NO se declara resuelto** — solo la causa primaria (que afectaba a todos) lo está.

Impacto: PR-B de Parties materializó las 9 tablas v2 con migración limpia gracias a `PartiesDbContextFactory`. NamingSeries sigue con su migración diferida.

### Actualización (jun 2026) — el MISMO bug en RUNTIME: 500 de login

El patrón "Wolverine + resolución de servicios scoped fuera de scope" **nos mordió por segunda vez**,
ahora en **runtime**. El login devolvía **500** (`api.identity.IssueJwtTokens`):
`Cannot resolve scoped service 'IEnumerable<ISaveChangesInterceptor>' from root provider` — la MISMA
excepción que en design-time, distinta superficie.

**Causa raíz (idéntica familia):** el helper `AddDbContextWithWolverineIntegration` (WolverineFx
6.8) registra las `DbContextOptions` con lifetime **SINGLETON**, así que el callback `(sp, options)`
recibe el **root provider**. La línea `options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>())`
—copiada de `AddHeroDbContext`, que usa `AddDbContext` con options **scoped** y por eso funciona—
intenta resolver los interceptores scoped (`AuditableEntity`/`DomainEvents`) desde root y lanza.
Estaba **idéntica en los 6 módulos** enrolados con Wolverine (Identity, Chat, Files, NamingSeries,
Notifications, Webhooks) → bomba latente sistémica; Identity fue el primero en dispararla (login).

**Fix:** quitar esa línea de los 6 callbacks. Con options singleton, EF Core **auto-descubre** los
`ISaveChangesInterceptor` registrados en DI y los aplica **desde el scope del contexto** al crearlo
(no en el build de las options) → los interceptores siguen funcionando (audit/domain-events
verificados con los tests de integración de Webhooks/Notifications/Identity). WolverineFx 6.8 **no
expone `optionsLifetime`** en estas sobrecargas, así que no se puede pedir options scoped directamente.

**Por qué los tests no lo atrapaban (deuda de clase, no de este bug):** el host de tests + dev usan
eventing **InMemory**; aunque registran el helper Wolverine, la ruta que dispara la resolución
problemática no se ejercía igual. **Toda la familia de bugs "Wolverine + DI scoped" es invisible a
la suite actual** (no corre la durabilidad Wolverine real). Es la lección que más importa: no es un
bug puntual, es una **clase de fallos de runtime** que la suite no ve. Deuda: un smoke de login con
el host corriendo Wolverine real.

### Lección UNIFICADA (design-time + runtime)

> Cualquier `DbContext` con integración Wolverine (`AddDbContextWithWolverineIntegration`) necesita:
> (1) una **`IDesignTimeDbContextFactory`** propia (para que `dotnet ef` no caiga en la enumeración
> envenenada — design-time), y (2) **NO resolver servicios scoped en el callback de options**
> (porque son singleton/root — runtime). EF auto-aplica los interceptores DI desde el scope; no hay
> que añadirlos a mano. La consolidación de los 6 callbacks idénticos en UN helper compartido
> (`AddHeroDbContextWithWolverine`) es la forma de que el próximo módulo no recopie ninguno de los
> dos defectos — pendiente (tocaría `BuildingBlocks`, §9).

## Riesgos

1. 🟠 **Discovery de migraciones**: ver §Deuda conocida. Bloquea la primera migración del módulo. Mitigación: handcraft en PR2 o nuevo experimento.
2. 🟡 **Cross-DbContext shared tx**: el allocator opera con SQL crudo sobre la conexión del caller, no con `NamingSeriesDbContext`. Patrón menos canónico que EF puro — requiere tests de integración rigurosos en PR2 (#1 de la lista del Plan Mode: 50 tasks concurrentes obtienen números distintos consecutivos sin huecos).
3. 🟢 **Patrón muy ad-hoc para algunos módulos**: si una entidad futura no es fiscal pura (ej. WarrantyCase con numeración interna), el rango bounded puede ser excesivo. Mitigación: marcar `ValidUntil=NULL`, `To=int.MaxValue` — la serie funciona como contador puro sin restricción DIAN.
