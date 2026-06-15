# Wolverine Fase 1 — Follow-ups y memoria institucional

> ADR-0001 (Outbox por módulo) + ADR-0004 (Bus = Wolverine) — Fase 1 cerró con la
> infraestructura cableada y verificada; la **demostración positiva de atomicidad
> estructural** quedó diferida a Fase 2.
>
> Este documento es la memoria institucional de las capas reveladas en Fase 1. Fase 2
> lo debe leer antes de empezar para no repetir descubrimientos.

---

## Las cuatro capas reveladas y resueltas en Fase 1

### Capa 1 — Setup del schema Wolverine (no usa migraciones EF)

**Síntoma**: `Npgsql.PostgresException 42P01: relation "identity.wolverine_outgoing_envelopes" does not exist` después de las migraciones EF.

**Causa**: Wolverine 6.8 gestiona su propio schema vía `AutoBuildMessageStorageOnStartup`, no EF migrations. En tests, el host arranca **antes** de que las migraciones EF creen el schema `identity`, así que `AutoCreate` durante `StartAsync` no encuentra el schema target y no produce las tablas. Las APIs `IMessageStore.Admin.MigrateAsync()` y `Admin.RebuildAsync()` invocadas manualmente post-migración resultaron no-op silenciosas — el path soportado en Wolverine 6.8 para crear las tablas es exclusivamente `AutoBuildMessageStorageOnStartup` durante `WolverineRuntime.StartAsync`.

**Resolución**: helper `WolverineSchemaSetup.ApplyAsync(connectionString, logger, ct)` en `src/Host/FSH.Starter.DbMigrator/WolverineSchemaSetup.cs` que **construye un mini-host efímero** con `UseWolverine` + `AutoBuildMessageStorageOnStartup = CreateOrUpdate`, lo arranca (Wolverine aplica DDL), verifica las 4 tablas, lo detiene. Invocado por:
- `DbMigrator.Program.cs` Step 2b (post-EF-migrations, pre-seed).
- `FshWebApplicationFactory.cs` post-`ProvisionRootTenantAsync` Step 5.

Cero duplicación de lógica entre prod y test.

`AutoBuildMessageStorageOnStartup` en el host del API queda **deshabilitado**: un solo path de bootstrap, no dos rivales.

---

### Capa 2 — Enrollment EF del DbContext con Wolverine

**Síntoma**: el host arrancaba con Wolverine cableado, pero `bus.PublishAsync` desde código de test producía cero envelopes en outgoing/incoming.

**Causa**: `services.AddDbContext<IdentityDbContext>(...)` (vía `AddHeroDbContext`) **no** enrola el DbContext en el outbox transaccional de Wolverine. La API canónica de Wolverine 6.8 es **alternativa** (no compositiva): `services.AddDbContextWithWolverineIntegration<T>(optionsBuilder, wolverineDatabaseSchema)` reemplaza al `AddDbContext` y registra el DbContext con la integración del outbox EF.

**Resolución**: en `src/Modules/Identity/Modules.Identity/IdentityModule.cs` se reemplazó `services.AddHeroDbContext<IdentityDbContext>()` por `services.AddDbContextWithWolverineIntegration<IdentityDbContext>(...)` replicando inline la configuración que `AddHeroDbContext` aplicaba (provider, connection string, migrations assembly, interceptors). La duplicación es deliberada y acotada a Identity — tocar `AddHeroDbContext` en `BuildingBlocks/Persistence` queda fuera del alcance Fase 1.

Tres invocaciones canónicas adicionales en `src/Host/FSH.Starter.Api/Program.cs` dentro del callback `UseWolverine`:
- `opts.PersistMessagesWithPostgresql(identityConnectionString, "identity")` — outbox sobre Postgres en el schema del módulo.
- `opts.UseEntityFrameworkCoreTransactions()` — enrola los DbContexts registrados con la integración EF.
- `opts.Policies.UseDurableLocalQueues()` — local queues durables.

Nota de orden: Wolverine 6.8 exige que los DbContexts se registren **antes** de `UseEntityFrameworkCoreTransactions` en la misma `IServiceCollection`. `IdentityModule` corre durante `AddModules()`, antes de `builder.Build()` que es cuando el callback `UseWolverine` se materializa — el orden está garantizado por el flujo del host.

---

### Capa 3 — API canónica out-of-handler para tests directos

**Síntoma**: tras Capa 2 cableada, `bus.PublishAsync(msg)` + `db.SaveChangesAsync()` desde un test que resolvía `IMessageBus` y `IdentityDbContext` del scope DI **seguía** sin producir envelope.

**Causa**: el codegen de Wolverine intercepta `IMessageBus.PublishAsync` para enrolar en outbox **solo dentro de handlers** (Wolverine reescribe el cuerpo del handler). Fuera de handlers (en código de un test que resuelve servicios directos), `PublishAsync` rutea al transporte local in-memory sin tocar el outbox del DbContext. La API canónica out-of-handler es `IDbContextOutbox<TDbContext>` con `SaveChangesAndFlushMessagesAsync()`.

**Resolución**: descartada como vía de smoke en Fase 1. `IDbContextOutbox<T>` es la API correcta para código out-of-handler, pero el escenario que demuestra ADR-0001 en la práctica son los handlers reales de los módulos (Fase 2) — no hay publicador out-of-handler en producción que valga la pena probar con esta API en Fase 1. Documentado aquí para Fase 2 / Fase 3 cuando aparezca un caso (caches refresh, jobs, etc.).

---

### Capa 4 — Routing y discovery del handler test-only

**Síntoma**: para probar atomicidad con un mensaje propio del test, necesitábamos extender el discovery de Wolverine sin tocar `Program.cs`.

**Causa**: el `Program.cs` del API hace `opts.Discovery.DisableConventionalDiscovery()` + `opts.Discovery.IncludeAssembly(Identity)` — el assembly de tests no se escanea. Cualquier handler test-only es invisible para Wolverine.

**Resolución**: `services.ConfigureWolverine(Action<WolverineOptions>)` es la API canónica de Wolverine para test overrides (doc oficial: *"Useful for testing overrides or for splitting configuration between modules"*). Es **additive** — se compone con el `UseWolverine` del Program.cs. El factory invoca:

```csharp
services.ConfigureWolverine(opts =>
{
    opts.Discovery.IncludeType(typeof(Phase1SmokeMessageHandler));
    opts.Discovery.IncludeType(typeof(Phase1SmokeChildMessageHandler));
});
```

`IncludeType(Type)` agrega **un solo tipo handler** — no escanea el assembly entero. Cero contaminación con otros tests.

Verificado por el test `WolverineWiringE2ETests.Invoking_Phase1SmokeMessage_Should_Execute_Through_Wolverine_Codegen`: la stack trace de la excepción del handler contiene `Internal.Generated.WolverineHandlers.Phase1SmokeMessageHandler1840323377.HandleAsync`, prueba de que el codegen está activo sobre el handler descubierto vía el override del factory.

---

## La 5ª capa — refinada en pre-Fase 2 (1 hipótesis viva, 4 descartadas)

**Síntoma original (Fase 1)**: dentro de un handler real (descubierto vía Capa 4, codegen activo según las stack traces), **ningún patrón documentado de Wolverine 6.8 produjo un envelope persistido visible al test** en `wolverine_outgoing_envelopes` ni en `wolverine_incoming_envelopes`. Patrones probados, todos con `outgoing total=0` y `incoming total=0`:

1. `IMessageBus bus` inyectado al handler + `await bus.PublishAsync(child)` — el publish corre, no falla, no persiste.
2. Mismo patrón con `DeliveryOptions { ScheduledTime = DateTimeOffset.UtcNow.AddHours(1) }` — sin efecto.
3. **Cascading message pattern**: `return new Phase1SmokeChildMessage(...)` desde el handler — sin efecto.
4. Combinado con handler stub `Phase1SmokeChildMessageHandler` registrado via `IncludeType` para darle ruta al child — sin efecto.

El codegen está activo (la stack trace lo prueba). Las tablas son consultables (el `WolverineSchemaSetup` verifica las 4). Pero el outbox no captura los mensajes producidos desde el handler.

### Estado actual de la 5ª capa (post-verificación previa + mini-tarea cero)

- **Hipótesis #1 — `[Transactional]` attribute en el handler.** ❌ **Descartada** por verificación previa pre-Fase 2 (Q1). La doc oficial de `Wolverine.Attributes` describe el atributo como *"In place of using [Transactional] attributes, apply transactional middleware to every message handler that uses transactional services"* — está pensado para handlers Wolverine, no para command handlers de Mediator (que es la forma del repo). Cita: `M:Wolverine.IPolicies.AutoApplyTransactions` XML doc.

- **Hipótesis #2 — `opts.OptimizeArtifactWorkflow()`.** ❌ **Descartada** por verificación previa pre-Fase 2 (Q1). La API **ya no existe** en Wolverine 6.8.0 ni en JasperFx 2.8.2 (removida/renombrada). El reemplazo aparente es `JasperFxOptions` vía `services.CritterStackDefaults(...)` — pero su propósito es configuración de codegen, no del outbox EF.

- **Hipótesis #3 — Transporte real (RabbitMQ activo) cambia la semántica del outbox.** ⚠️ **Parcialmente descartada**. La verificación previa Q1 confirma que la persistencia en outgoing ocurre dentro de `SaveChangesAndFlushMessagesAsync` con o sin transporte; el transporte solo afecta el flush post-commit hacia RabbitMQ. Por tanto la 5ª capa **no es por falta de transporte**. Sin embargo, Fase 2 sí activa RabbitMQ y validará empíricamente este punto al primer publicador real migrado.

- **Hipótesis #4 — `IncludeMessage<T>` ≠ `IncludeType<T>` para mensajes cascadeados.** ❌ **Descartada por irrelevancia**. Era específica del escenario sintético de Fase 1 (publicar un child message desde un handler de test). En Fase 2 los publicadores son command handlers de Mediator que usan `IDbContextOutbox<T>` directo — **no hay cascading**, no aplica.

- **Hipótesis #5 — Coexistencia de `ISaveChangesInterceptor` propios con el interceptor de Wolverine instalado por `AddDbContextWithWolverineIntegration`.** ⚠️ **Hipótesis viva, no probada**. Identificada en verificación previa pre-Fase 2, evaluada en **mini-tarea cero (Plan Mode, sin ejecución)**. El Plan Mode reveló que `IdentityModule` registra tres interceptores **críticos** vía `sp.GetServices<ISaveChangesInterceptor>()`:

  - `AuditableEntitySaveChangesInterceptor` ([AuditableEntitySaveChangesInterceptor.cs](src/BuildingBlocks/Persistence/Inteceptors/AuditableEntitySaveChangesInterceptor.cs)) — popula audit columns (`CreatedOnUtc/By`, `LastModifiedOnUtc/By`) y maneja soft delete + owned references.
  - `DomainEventsInterceptor` ([DomainEventsInterceptor.cs](src/BuildingBlocks/Persistence/Inteceptors/DomainEventsInterceptor.cs)) — despacha domain events de `IHasDomainEvents` vía Mediator `IPublisher` post-SaveChanges.
  - `AuditingSaveChangesInterceptor` ([AuditingSaveChangesInterceptor.cs](src/Modules/Auditing/Modules.Auditing/Persistence/AuditingSaveChangesInterceptor.cs)) — captura cambios de entidad y produce `EntityChange` audit log cross-cutting.

  Quitarlos rompe auditoría, domain events y audit log — **costo arquitectónico real, no quirúrgico**. La hipótesis se reformuló: no es "quitar interceptores", sino **"cómo coexisten interceptores de auditoría/domain-events del proyecto con el outbox transaccional de Wolverine"**. Decisión: diferir a Fase 2 cuando un publicador real (`UserRegistered`) revele la causa raíz con datos reales en juego. La respuesta canónica probablemente exista en doc de Wolverine (Marten + auditing es escenario común) — investigar al toparla, no antes. **Mini-tarea cero no tocó código, working tree limpio, sin deuda nueva.**

### Conclusión

Progreso: la 5ª capa pasó de **"5 hipótesis pendientes"** a **"1 hipótesis viva (coexistencia interceptors ↔ outbox EF)"**. Las otras 4 están descartadas con evidencia. La hipótesis viva se valida con publicador real en Fase 2, no con smoke sintético. **ADR-0001 sigue en estado "cableado, atomicidad observable diferida a Fase 2"** — sin cambio.

### Lecciones de la verificación previa

Investigar la API real **antes** de implementar (verificación previa pre-Fase 2 con XML docs + búsquedas en doc oficial) descartó 3 hipótesis (#1, #2, #4) y refinó 2 más (#3 parcialmente, #5 reformulada) en ~15 minutos read-only. Este patrón ahorró potencialmente una a dos rondas completas de implementación-y-revertir. **Repetir en futuras fases ante incertidumbre técnica**: una hora de doc + assembly inspection antes de tocar código, no después. La mini-tarea cero (Plan Mode obligatorio antes de implementar) además interceptó un cambio que habría roto auditoría y domain events del proyecto — confirma que el costo de detenerse a leer es siempre menor que el costo de revertir.

---

## Lo que Fase 1 sí dejó verificado

| Pieza | Verificación |
|---|---|
| **4 tablas Wolverine** en schema `identity` | `WolverineWiringE2ETests.Wolverine_Schema_Tables_Should_Exist_In_Identity_Schema` — pasa. |
| **Helper compartido prod ↔ tests** | `WolverineSchemaSetup.ApplyAsync` invocado por `DbMigrator` Step 2b y `FshWebApplicationFactory`. Log "confirmed 4/4 tables in schema 'identity'" en ambos paths. |
| **Enrollment EF cableado** | 3 invocaciones canónicas en `Program.cs` + `AddDbContextWithWolverineIntegration` en `IdentityModule.cs`. Build verde, host arranca limpio. |
| **Discovery additive test-only** | `services.ConfigureWolverine` + `IncludeType` en el factory. No contamina otros tests. |
| **Codegen activo sobre el handler** | `WolverineWiringE2ETests.Invoking_Phase1SmokeMessage_Should_Execute_Through_Wolverine_Codegen` — stack trace contiene `Internal.Generated.WolverineHandlers.Phase1SmokeMessageHandler1840323377.HandleAsync`. |
| **Bus propio sigue activo** | Architecture.Tests 51/51 verdes. Los 5 publicadores `IOutboxStore.AddAsync` y los 4 consumidores `IIntegrationEventHandler` no fueron tocados — siguen operando en paralelo a Wolverine. |
| **Convivencia con Mediator** | Mediator source-gen 3.0.2 sigue atendiendo comandos in-process. Wolverine **solo** está en el assembly de Identity para discovery + el handler test-only via override del factory. Ningún `bus.InvokeAsync` en código de producción. |

---

## Lo que NO se hizo (decisiones cerradas, no diferidas)

- **Setup de Wolverine vivirá siempre en DbMigrator**, no como AutoCreate en el API. Un solo path de bootstrap. Decisión cerrada.
- **Sin tocar `BuildingBlocks/Eventing`** — el bus propio (`IEventBus`, `IOutboxStore`, `OutboxDispatcher`, `EventingArchitectureTests`) sigue vivo y será desmantelado en Fase 3 cuando los 4 consumidores migren.
- **Sin tocar los 5 publicadores** `IOutboxStore.AddAsync` — migran en Fase 2.
- **Sin tocar los 4 consumidores** `IIntegrationEventHandler` — migran en Fase 3.
- **MassTransit v8.5.7 sigue activo** (Apache 2.0) — no migra a v9.
