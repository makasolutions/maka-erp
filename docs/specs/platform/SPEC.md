# SPEC · Fundación de Plataforma — Maka ERP   (AS-BUILT)

> **Estado:** As-built (refleja el código en `develop` @ `3214a0ef`, jun-2026).
> **Metodología:** Spec-Driven + Harness · **Origen:** lectura del código del repo.
> **Constitución:** `docs/specs/_constitution.md` (índice maestro). `_TEMPLATE.md` aún no existe — las
> invariantes de §1 son las heredadas del índice maestro §4. ADRs reales: `docs/adr/0001`, `docs/adr/0002`.
> **Alcance:** los cimientos transversales — **Multitenancy (Finbuckle)**, **Outbox/Eventing**,
> **BuildingBlocks/kernel** — de los que dependen todos los módulos. Es la dependencia directa de
> **Inventory** (que necesitará emitir eventos), por eso se documenta antes de su forward.

---

## 0. Cómo leer

As-built: describe lo que la plataforma **provee hoy**. La sección "Tasks" se reemplaza por **§6 Estado
actual, huecos y deuda**. El entregable de mayor valor es **§6 → ADR-0001 (topología de Outbox)**: la
descripción precisa de la topología actual es de lo que depende el forward de Inventory. No se resuelve
ningún ADR aquí.

---

## 1. Constitución aplicable

| Invariante | Estado | Evidencia |
|---|---|---|
| **INV-3** — append-only en ledgers; el outbox es append-and-mark, nunca edición destructiva del evento | **CUMPLE** | `OutboxMessage` solo muta columnas de **bookkeeping** (`ProcessedOnUtc`, `RetryCount`, `IsDead`, `LastError`); `Type`/`Payload`/`TenantId` nunca se reescriben (`EfCoreOutboxStore.cs:31-83`). El payload del evento es inmutable post-`AddAsync`. |
| **INV-8** — integración por eventos vía Outbox; prohibido `IEventBus` directo (con Architecture.Test que lo protege) | **CUMPLE + BLINDADO** | Tras el fix `2162f6c7`, **0** usos de `IEventBus` en `src/Modules` (solo el `OutboxDispatcher` y los buses de BuildingBlocks lo tocan). Protegido por `Architecture.Tests/EventingArchitectureTests` (`3214a0ef`): ningún tipo de módulo puede depender del tipo `IEventBus` (prueba bidireccional roja/verde documentada). |
| **INV-9** — multitenant: `OwnerId Guid?`; tenant restaurado en scope fresco al consumir eventos | **CUMPLE (restauración MANUAL, no infra)** | Finbuckle resuelve tenant por header (`MultitenancyModule.cs:88` `.WithHeaderStrategy(MultitenancyConstants.Identifier)`); filtro default-ON (`BaseDbContext.ApplyTenantIsolationByDefault`, `:41`). Restauración en consumidores: **patrón por-consumidor** (`MentionedInChannelIntegrationEventHandler.cs:63-74`, `WebhookFanoutHandler`, `FshJobActivator.cs:40`). **No hay** auto-restore en el dispatcher → ADR-0001. `OwnerId Guid?` existe en dominios de negocio (p.ej. `Product`), no en el kernel. |
| **INV-10** — convenciones FSH (namespaces; `Guid.CreateVersion7()`; `base.OnModelCreating` al final; Mediator source-gen) | **CUMPLE** | Namespaces `FSH.Framework.*` (BuildingBlocks) / `FSH.Modules.*`. `Guid.CreateVersion7()` en entidades. `BaseDbContext` aplica isolation tras Finbuckle (`:36-41`); subclases llaman `base.OnModelCreating` al final (verificado en Catalog/Lookups/Parties). Mediator source-gen (`o.ServiceLifetime = Scoped`, `Program.cs:36`). |
| **ADR-0002** — `OccurredOnUtc` es `DateTimeOffset` (opción A, cero migración) | **CUMPLE** | `IIntegrationEvent.OccurredOnUtc : DateTimeOffset` (`Eventing.Abstractions/IIntegrationEvent.cs`, commit `a32e50b3`); columnas bookkeeping siguen `DateTime` vía `.UtcDateTime` (`EfCoreOutboxStore.cs:39`). |

---

## 2. Glosario

| Término | Tipo real | Qué es |
|---|---|---|
| **Tenant** | `AppTenantInfo` (`Shared/Multitenancy/AppTenantInfo.cs`) | Identidad de la empresa cliente; resuelto por header `tenant` (Finbuckle). Tenants especiales: `Root`, `Global` (`MultitenancyConstants`). |
| **`OwnerId Guid?`** | campo en entidades de negocio | Preparación para propiedad cross-tenant (marketplace); no activado como gate. |
| **Outbox** | `OutboxMessage` + `IOutboxStore` + `OutboxDispatcher` | Cola transaccional de integration events: se escribe junto al cambio, se entrega async. |
| **IntegrationEvent** | `IIntegrationEvent` (`Eventing.Abstractions`) | Evento cross-módulo (`Id, OccurredOnUtc, TenantId, CorrelationId, Source`). |
| **Dispatcher** | `OutboxDispatcher` + `OutboxDispatcherHostedService` | Lee el lote pendiente y publica vía `IEventBus`; corre en background cada N s. |
| **Scope fresco** | `IServiceScopeFactory.CreateScope()` + `IMultiTenantContextSetter` | Mecanismo para restaurar el tenant antes de resolver un DbContext en background. |
| **IEventBus** | interfaz (`InMemoryEventBus` / `RabbitMqEventBus`) | **Reservado a BuildingBlocks**; los módulos NO pueden depender de él. |
| **IOutboxStore** | interfaz | La **única** vía de publicación desde un módulo (`AddAsync`). |
| **Inbox** | `InboxMessage` + `IInboxStore` | Idempotencia del consumidor por `{eventId, handlerName}`. |
| **BaseEntity / AggregateRoot** | `Core/Domain/*` | `BaseEntity<TId>` = `Id` + domain events; `AggregateRoot<TId>` = marcador de raíz. |

---

## 3. Alcance as-built

### 3.1 Qué provee hoy
1. **Multitenancy (Finbuckle 10.x)**: resolución de tenant por header, filtro de aislamiento default-ON en
   `BaseDbContext`, opt-out vía `IGlobalEntity`, store de tenants (`TenantDbContext`), provisioning + theme,
   helpers de restauración de tenant (`IMultiTenantContextSetter`) usados por jobs/consumidores.
2. **Outbox/Eventing**: publicación transaccional (`IOutboxStore.AddAsync`), dispatcher background con
   retry/dead-letter, Inbox para idempotencia, bus conmutable InMemory (dev) / RabbitMQ (prod), límite
   arquitectónico `IEventBus`-prohibido-en-módulos.
3. **BuildingBlocks/kernel**: `BaseEntity<TId>`/`AggregateRoot<TId>`, interfaces de dominio
   (`IHasTenant`, `IAuditableEntity`, `ISoftDeletable`, `IGlobalEntity`, `IHasDomainEvents`), Mediator
   source-gen, persistencia base, caching/jobs/storage/quota/web (fuera del foco de este spec).

### 3.2 Qué NO provee / está diferido
- **Outbox atómico por módulo**: hoy es central (§6/ADR-0001) → atomicidad real solo en Identity.
- **Auto-restore de tenant en el dispatcher**: hoy es manual por consumidor (INV-9/ADR-0001).
- **MassTransit**: **no está en el código** (ver §6 discrepancia). El bus RabbitMQ es propio (`RabbitMQ.Client`).
- **Repositorio genérico**: no existe (`IRepository<T>` = 0 usos) — por diseño (DbContext directo).

### 3.3 Quién depende (consumidores actuales)
- **Publicadores vía Outbox (5 sitios):** Chat `SendMessageCommandHandler`, Files
  `FinalizeUploadCommandHandler`, Identity `UserRegisteredEventHandler` + `GenerateTokenCommandHandler` +
  `UserRegistrationService`.
- **Consumidores (`IIntegrationEventHandler<T>`, 4 concretos):** Identity `TokenGeneratedLogHandler`,
  Identity `UserRegisteredEmailHandler`, Notifications `MentionedInChannelIntegrationEventHandler`,
  Webhooks `WebhookFanoutHandler` (genérico, fan-out a suscripciones).
- **Todos los DbContext de módulo** heredan `BaseDbContext` → dependen del filtro de tenant del kernel.

---

## 4. Requirements (EARS)

### CAP-01 · Multitenancy
- **REQ-01.1** CUANDO llega un request EL SISTEMA DEBERÁ resolver el tenant desde el header `tenant`
  (Finbuckle header-strategy), **antes** de `UseAuthentication`. *(`MultitenancyModule.cs:81,88`; `Program.cs:101` `UseHeroMultiTenantDatabases`)*
- **REQ-01.2** EL SISTEMA DEBERÁ aplicar un filtro de aislamiento de tenant a toda entidad por defecto,
  con opt-out solo vía `IGlobalEntity`. *(`BaseDbContext.ApplyTenantIsolationByDefault`, `:41`)*
- **REQ-01.3** SI un proceso en background (job/dispatcher) necesita un DbContext tenant-filtrado ENTONCES
  EL SISTEMA DEBERÁ restaurar el tenant en un scope fresco vía `IMultiTenantContextSetter` **antes** de
  resolver el contexto. *(`MentionedInChannelIntegrationEventHandler.cs:63-74`, `FshJobActivator.cs:40-41`)*
- **REQ-01.4** DONDE una entidad de negocio lo prepare EL SISTEMA DEBERÁ ofrecer `OwnerId Guid?` para
  propiedad cross-tenant (no activado como gate). *(campo en dominios de negocio)*

### CAP-02 · Outbox / Eventing
- **REQ-02.1** CUANDO un handler de módulo publica un integration event EL SISTEMA DEBERÁ persistirlo vía
  `IOutboxStore.AddAsync` (no `IEventBus`). *(`IOutboxStore.cs:10`; INV-8)*
- **REQ-02.2** CUANDO se llama `AddAsync` EL SISTEMA DEBERÁ serializar el evento y hacer `SaveChanges`
  inmediato sobre el DbContext del store. *(`EfCoreOutboxStore.cs:48-49`)* — *(matiz de atomicidad: §6/ADR-0001)*
- **REQ-02.3** MIENTRAS haya mensajes pendientes (`ProcessedOnUtc == null && !IsDead`) EL SISTEMA DEBERÁ,
  cada `OutboxDispatchIntervalSeconds`, leer un lote (`OutboxBatchSize`, def. 100) y publicarlos vía
  `IEventBus`. *(`OutboxDispatcher.cs:37-64`, `OutboxDispatcherHostedService.cs:26-55`)*
- **REQ-02.4** SI la publicación de un mensaje falla ENTONCES EL SISTEMA DEBERÁ incrementar `RetryCount` y,
  superado `OutboxMaxRetries` (def. 5), marcarlo `IsDead` (dead-letter). *(`OutboxDispatcher.cs:74`, `EfCoreOutboxStore.MarkAsFailedAsync`)*
- **REQ-02.5** CUANDO un consumidor procesa un evento EL SISTEMA DEBERÁ aplicar idempotencia por
  `{eventId, handlerName}` (Inbox), saltando si ya procesado. *(`InMemoryEventBus.cs:93-114`)*
- **REQ-02.6** EL SISTEMA DEBERÁ seleccionar el bus por `EventingOptions.Provider`: `RabbitMQ` →
  `RabbitMqEventBus`; cualquier otro → `InMemoryEventBus` (default). *(`ServiceCollectionExtensions.cs:35-41`)*
- **REQ-02.7** SI un tipo de un assembly de módulo depende de `IEventBus` ENTONCES el build de arquitectura
  DEBERÁ fallar. *(`EventingArchitectureTests`)* — INV-8 blindada.

### CAP-03 · BuildingBlocks / kernel
- **REQ-03.1** EL SISTEMA DEBERÁ proveer `BaseEntity<TId>` con **solo** `Id` + domain events
  (`AddDomainEvent`/`ClearDomainEvents`); sin columnas de tenant/auditoría. *(`Core/Domain/BaseEntity.cs:7-31`)*
- **REQ-03.2** EL SISTEMA DEBERÁ proveer `AggregateRoot<TId>` como marcador de raíz de agregado.
  *(`Core/Domain/AggregateRoot.cs:7`)*
- **REQ-03.3** EL SISTEMA DEBERÁ estampar tenant/auditoría/soft-delete por interceptor vía las interfaces
  `IHasTenant`/`IAuditableEntity`/`ISoftDeletable`, no por campos en `BaseEntity`. *(`Core/Domain/*`)*
- **REQ-03.4** EL SISTEMA DEBERÁ NO proveer repositorio genérico; los handlers inyectan el DbContext del
  módulo. *(0 usos de `IRepository`)*

---

## 5. Design as-built

### 5.1 Tipos y contratos (firmas reales)
```csharp
// Eventing.Abstractions
interface IIntegrationEvent { Guid Id; DateTimeOffset OccurredOnUtc; string? TenantId;
                              string CorrelationId; string Source; }     // ADR-0002
interface IIntegrationEventHandler<in T> { Task HandleAsync(T @event, CancellationToken ct); }

// Eventing.Outbox
interface IOutboxStore {
    Task AddAsync(IIntegrationEvent @event, CancellationToken ct = default);
    Task<IReadOnlyList<OutboxMessage>> GetPendingBatchAsync(int batchSize, CancellationToken ct = default);
    Task MarkAsProcessedAsync(OutboxMessage message, CancellationToken ct = default);
    Task MarkAsFailedAsync(OutboxMessage message, string error, bool isDead, CancellationToken ct = default);
}
class OutboxMessage { Guid Id; DateTime CreatedOnUtc; string Type; string Payload; string? TenantId;
                      string? CorrelationId; DateTime? ProcessedOnUtc; int RetryCount; string? LastError; bool IsDead; }
class InboxMessage  { Guid Id; string EventType; string HandlerName; DateTime ProcessedOnUtc; string? TenantId; }

// Core/Domain
abstract class BaseEntity<TId> : IEntity<TId>, IHasDomainEvents { TId Id; IReadOnlyCollection<IDomainEvent> DomainEvents; … }
abstract class AggregateRoot<TId> : BaseEntity<TId> { }
```

### 5.2 Flujo de publicación (con el punto exacto de restauración de tenant)
```
[handler de módulo]  ── outbox.AddAsync(evt) ──►  OutboxMessage (SaveChanges en el DbContext del store)
        │  (mismo request, tenant del request)
        ▼
[OutboxDispatcherHostedService]  cada N s  ── CreateScope ──►  OutboxDispatcher.DispatchAsync
        │                                                         │ GetPendingBatchAsync(100)
        │                                                         │ _bus.PublishAsync(evt)   ← SIN tenant ambiente
        ▼                                                         ▼
[InMemoryEventBus]  CreateScope ── Inbox dedupe {id,handler} ──►  IIntegrationEventHandler<T>.HandleAsync
                                                                  │  ⚠️ AQUÍ el consumidor DEBE:
                                                                  │  CreateScope + IMultiTenantContextSetter(evt.TenantId)
                                                                  │  ANTES de resolver su DbContext
                                                                  ▼  (si no → fuga/NRE silenciosa — eventing.md)
```
- **Atomicidad:** `AddAsync` hace su propio `SaveChanges` → atómico con el cambio de dominio **solo** si
  comparten DbContext (caso Identity). Para Chat/Files: durable, no atómico (§6/ADR-0001).
- **Restauración de tenant:** **manual en cada consumidor**, no en el dispatcher.

### 5.3 Configuración
- `EventingOptions` (`EventingOptions.cs`): `Provider="InMemory"` · `OutboxBatchSize=100` ·
  `OutboxMaxRetries=5` · `EnableInbox=true` · **`OutboxDispatchIntervalSeconds=10` (default)** —
  **override a `3` en host** (`appsettings.json` + `appsettings.Development.json`, fix `2162f6c7`) ·
  `UseHostedServiceDispatcher=true`.
- **Bus prod:** `RabbitMqEventBus` usa **`RabbitMQ.Client` directo** (`RabbitMq/RabbitMqEventBus.cs:4,179`),
  **no MassTransit** (§6).
- **Finbuckle** (`MultitenancyModule.cs:60-88`): `.AddMultiTenant<AppTenantInfo>()` + `.WithHeaderStrategy`
  + store en `TenantDbContext`; `app.UseMultiTenant()` (`Extensions.cs:15`) corre **antes** de auth.
- **Registro eventing** (`ServiceCollectionExtensions.cs`): `AddEventingCore` (serializer singleton + bus +
  hosted dispatcher) · `AddEventingForDbContext<T>` (store/inbox/dispatcher **scoped**, `:62-64`) ·
  `AddIntegrationEventHandlers(assembly)`.

### 5.4 Persistencia del outbox
- Tabla `OutboxMessage` — **único `DbSet<OutboxMessage>` del repo, en `IdentityDbContext`**. Columnas en
  §5.1. Lectura de pendientes: `Where(!IsDead && ProcessedOnUtc==null)` ordenado por `CreatedOnUtc`
  (`EfCoreOutboxStore.cs:52-55`). `OutboxMessage`/`InboxMessage` son `IGlobalEntity` (sin filtro de tenant;
  `TenantId` es columna explícita que el consumidor restaura). Índices: no verificados en este pase
  (revisar `OutboxMessage` config si existiera) — **anotado como hueco de verificación**.

---

## 6. Estado actual, huecos y deuda técnica

### Por capacidad
| Capacidad | Estado |
|---|---|
| Multitenancy (resolución, filtro, restauración manual) | ✅ Operando; restauración en consumidor = patrón manual (no infra) |
| Outbox publish/dispatch/retry/dead-letter | ✅ Operando |
| Inbox idempotencia | ✅ Operando (`InMemoryEventBus`) |
| Bus RabbitMQ (prod) | ⚠️ Implementado con `RabbitMQ.Client`; **no MassTransit** (discrepancia doc) |
| Atomicidad outbox por módulo | ❌ Ausente (central) → ADR-0001 |
| Auto-restore de tenant en dispatcher | ❌ Ausente → ADR-0001 |

### ⚠️ ADR-0001 (topología de Outbox) — DECISIÓN ABIERTA Y BLOQUEANTE

**Topología ACTUAL (descrita con precisión — entregable clave):**
- **Outbox central, único, mono-DbContext.** Hay **una** tabla `OutboxMessage`, y su **único**
  `DbSet<OutboxMessage>` vive en `IdentityDbContext`.
- `IOutboxStore` se registra **sin key** (`AddScoped<IOutboxStore, EfCoreOutboxStore<TDbContext>>`,
  `ServiceCollectionExtensions.cs:62`); en la composición real el ganador es
  `EfCoreOutboxStore<IdentityDbContext>` (last-wins si otro módulo registrara). Todos los publicadores
  (Chat/Files/Identity) escriben a **ese mismo** store.
- **Atomicidad:** `EfCoreOutboxStore.AddAsync` hace su **propio** `SaveChanges` (`:48-49`). La fila del
  outbox entra en la **misma transacción** que la mutación de negocio **solo cuando comparten DbContext** —
  hoy únicamente Identity. Para Chat/Files es **dual-write durable pero NO atómico**: si el proceso muere
  entre el `SaveChanges` del módulo y el del outbox, queda mutación sin evento (o evento huérfano).
- **Restauración de tenant:** el dispatcher publica **sin tenant ambiente**; cada consumidor debe
  restaurarlo **manualmente** (hoy lo hacen Notifications y Webhooks). Nada lo fuerza a nivel infra.

**Decisión pendiente (no resolver aquí):**
1. **Outbox por módulo (atómico)** — fila del outbox en el `DbSet<OutboxMessage>` de **cada**
   `{Module}DbContext`, store tipado/keyed, dispatcher multi-contexto. Atomicidad total. Costo: cambio de
   **BuildingBlocks** (protegido) + migración por módulo.
   **vs. Outbox central (actual)** — cero costo, atomicidad solo en Identity.
2. **Auto-restore de tenant en el dispatcher** — un decorator/base que instale `evt.TenantId` antes de
   invocar cualquier handler, para que ningún consumidor pueda olvidarlo (hoy es regla violable).

**Por qué bloquea el backbone de comercio:** para Chat/Files el dual-write no atómico es trivial (una
mención perdida). Para **Inventory/Orders/Billing-DIAN** un `OrderPlaced`/`StockMoved` huérfano o perdido
**descuadra inventario y contabilidad** — la atomicidad ahí **no es opcional**. Igual el tenant-scope:
un consumidor del backbone que olvide restaurar el tenant corrompe datos de forma **silenciosa**. Por eso
ambas decisiones se resuelven **antes** de implementar esos módulos. (Detalle completo en `docs/adr/0001`.)

### Discrepancias contra la constitución / documentación
1. **🔴 MassTransit no existe en el código.** CLAUDE.md/AGENTS.md afirman "MassTransit 8.5.7 (Apache 2.0)";
   el grep de `MassTransit` en `src/**` (`.cs` + `.csproj` + `Directory.Packages.props`) devuelve **0**.
   El bus de producción es `RabbitMqEventBus` sobre **`RabbitMQ.Client`** propio
   (`RabbitMq/RabbitMqEventBus.cs:4`). **Severidad media** (no es bug funcional, pero la constitución
   afirma una dependencia inexistente — corregir en CLAUDE.md/AGENTS.md, fuera de este spec).
2. **INV-9 cumplido por convención, no por construcción.** La restauración de tenant en consumidores es
   manual (ADR-0001 propone blindarla). Severidad media — riesgo de regresión silenciosa.
3. **Auditoría inconsistente aguas abajo:** módulos Maka usan `CreatedAtUtc` manual en vez de
   `IAuditableEntity` (hallazgo M6 del baseline Fase A); el kernel ofrece la interfaz correcta pero no se
   adopta uniformemente. Severidad baja (afecta a módulos, no al kernel).

### TODOs / code smells (archivo:línea)
- **0 `TODO`/`FIXME`/`NotImplementedException`** en `BuildingBlocks/Eventing`.
- **Smell — registro last-wins del store** (`ServiceCollectionExtensions.cs:62`): `AddScoped<IOutboxStore,…>`
  sin key permite que un segundo `AddEventingForDbContext` pise silenciosamente el store global (footgun
  documentado en ADR-0001).
- **Verificación pendiente:** índices de `OutboxMessage` (no confirmados en este pase).

---

## 7. Harness (verificación existente)

### 7.1 Requirement → test
| REQ | Test existente | Cobertura |
|---|---|---|
| 02.7 (no-IEventBus) | `Architecture.Tests/EventingArchitectureTests` (regla + control + fire-test bidireccional) | ✅ |
| 02.1–02.5 (publish/dispatch/retry/inbox) | — (ejercitado **indirectamente** por `Integration.Tests/Chat/MentionAndNotificationTests` vía `DispatchOutboxAsync`) | ⚠️ sin test unitario directo del dispatcher/store |
| 01.1–01.3 (tenant resolución/filtro/restauración) | `Architecture.Tests/TenantIsolationTests` (reglas de aislamiento) + `Multitenancy.Tests` (Domain/Handlers/Provisioning/`TenantLifecycleTests`) | ⚠️ aislamiento sí; **restauración en consumidor background = sin test** |
| 03.1–03.4 (kernel) | implícito en `Architecture.Tests` (DomainEntity/Namespace/Layer) | ⚠️ sin test de unidad propio de `BaseEntity` |

### 7.2 Architecture.Tests que protegen estos cimientos
- **`EventingArchitectureTests`** → INV-8 (ningún tipo de módulo depende de `IEventBus`; + control positivo
  de que `IIntegrationEvent` SÍ se permite).
- **`TenantIsolationTests`** → reglas de aislamiento de tenant en entidades.
- **`BuildingBlocksIndependenceTests` / `LayerDependencyTests` / `ContractsPurityTests` /
  `ModuleArchitectureTests` / `NamespaceConventionsTests` / `CircularReferenceTests`** → límites del kernel
  y de módulos. (Global: **51/51** verdes @ `3214a0ef`.)

### 7.3 Definition of Done (tests que faltan)
1. **Tenant-scope de consumidores**: un test que falle si un `IIntegrationEventHandler<T>` lee un DbContext
   tenant-filtrado **sin** restaurar el tenant (hoy es regla solo en `eventing.md`). Candidato a
   Architecture.Test (reflexión sobre consumidores) o integration test de fuga cross-tenant.
2. **Atomicidad del outbox**: test que demuestre el comportamiento dual-write (y, tras ADR-0001, que
   garantice atomicidad por-módulo).
3. **Dispatcher/store unit tests**: retry → dead-letter al superar `OutboxMaxRetries`; Inbox dedupe.
4. **Resolución de tenant por header** (integration): request con header `tenant` correcto/incorrecto.

---

## 8. Decisiones abiertas
1. **ADR-0001 — topología de Outbox (central vs por-módulo) + auto-restore de tenant en el dispatcher.**
   BLOQUEANTE antes de Inventory/Orders/Billing-DIAN. Estado y opciones en `docs/adr/0001`; resumen en §6.
2. **MassTransit vs RabbitMQ.Client propio**: la doctrina dice MassTransit, el código usa cliente propio.
   ¿Se corrige la doctrina (aceptar el bus propio) o se migra a MassTransit? Decisión de dueño (afecta
   CLAUDE.md/AGENTS.md). No bloquea el backbone si el bus propio cumple, pero la doc miente hoy.
3. **Blindar la restauración de tenant a nivel infra** (parte de ADR-0001) — convertir la regla violable en
   imposible de violar, como se hizo con `EventingArchitectureTests`.
4. **Adopción uniforme de `IAuditableEntity`** en módulos (M6) — consistencia transversal.

---

## 9. Trazabilidad
| REQ | Diseño (§5) | Test (§7) |
|---|---|---|
| 01.1–01.2 | Finbuckle header-strategy, `BaseDbContext` | ⚠️ aislamiento sí; resolución por header sin test directo |
| 01.3 (restauración) | `IMultiTenantContextSetter` en scope fresco | ❌ sin test |
| 01.4 (`OwnerId`) | campo en negocio | N/A (kernel no lo define) |
| 02.1–02.6 | `IOutboxStore`/`OutboxDispatcher`/`InMemoryEventBus`/`EventingOptions` | ⚠️ solo indirecto (mention test) |
| 02.7 | `EventingArchitectureTests` | ✅ |
| 03.1–03.4 | `Core/Domain/*` | ⚠️ vía Architecture.Tests, sin unit propio |

**REQ sin diseño claro:** ninguno. **REQ sin test:** 01.3 (tenant-restore en consumidor), atomicidad
outbox, dispatcher/store retry, resolución por header — los huecos críticos del harness de fundación.

---

*Documento as-built. Cero archivos de código modificados. El porqué de las decisiones: `docs/adr/0001`,
`docs/adr/0002`, `.agents/rules/eventing.md`, `.agents/rules/database.md`, `.agents/rules/architecture.md`.*
