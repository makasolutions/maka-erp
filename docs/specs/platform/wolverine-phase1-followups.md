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

## La 5ª capa — CERRADA con causa raíz en Fase 2 (was: 1 hipótesis viva, 4 descartadas)

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

- **Hipótesis #5 — Coexistencia de `ISaveChangesInterceptor` propios con un interceptor de Wolverine.** ❌ **Hipótesis INVALIDADA con evidencia en Fase 2** (investigación read-only contra source de WolverineFx.EntityFrameworkCore 6.8.0 + Fix variante (a) con E2E verde).

  Hallazgo definitivo de la investigación: **Wolverine NO usa `ISaveChangesInterceptor`**. La integración EF Core funciona vía **codegen frames inyectados en el cuerpo del handler Wolverine** durante la compilación — `EnrollDbContextInTransaction` al inicio + `CommitEfCoreEnvelopeTransaction` como postprocessor. Cita: `T:Wolverine.EntityFrameworkCore.Codegen.CommitEfCoreEnvelopeTransaction` — *"Commits the EF Core envelope transaction (committing the EF Core database transaction and then flushing the MessageContext's outgoing messages). Emitted as a postprocessor so it runs before the HTTP response writer, ensuring the outbox is flushed before the response is sent (GH-2917)."*

  **Los 3 interceptors propios (`AuditableEntity` / `DomainEvents` / `Auditing`) son ortogonales al outbox EF de Wolverine**. No hay colisión arquitectónica que resolver.

### Causa raíz REAL — bug de uso del API

  La 5ª capa fue causada por una asunción incorrecta sobre la semántica de `IDbContextOutbox<T>.PublishAsync`. Doc oficial (`M:IDbContextOutbox.FlushOutgoingMessagesAsync`):

  > *"Calling this method will force the outbox to send out any outstanding messages that were captured as part of processing the transaction **if you call SaveChangesAsync() directly on the DbContext**."*

  El flujo original era:

  ```csharp
  await outbox.PublishAsync(evt, delivery);   // encola en memoria del MessageContext
  await db.SaveChangesAsync(ct);              // persiste user — NO flushea outbox
  // FIN → envelope nunca toca wolverine_outgoing_envelopes
  ```

  El flujo correcto, una sola línea de diferencia:

  ```csharp
  await outbox.PublishAsync(evt, delivery);
  await outbox.SaveChangesAndFlushMessagesAsync(ct);   // commitea tx EF + flushea outbox
  ```

  Fix aplicado en Fase 2: la fachada `IIntegrationEventPublisher<TDbContext>` expone un método `SaveChangesAndFlushAsync(ct)` que internamente llama `dbContextOutbox.SaveChangesAndFlushMessagesAsync(ct)` — cubre uniformemente ambas rutas (Wolverine flushea outbox; Legacy persiste el `OutboxMessages` entity ya añadido por `EfCoreOutboxStore`). El `UserRegistrationService.PublishUserRegisteredAsync` reemplazó un único `db.SaveChangesAsync(ct)` por `integrationEventPublisher.SaveChangesAndFlushAsync(ct)`. **Una línea cambiada en el call site**.

### Verificación

  `WolverineUserRegisteredE2ETests.UserRegistration_Should_Publish_Envelope_And_Deliver_Via_RabbitMq` ✅ — registro de usuario por HTTP → envelope persistido en `wolverine_outgoing_envelopes` → entregado por RabbitMQ (Testcontainer) → recibido por consumer test-only con `TenantId` correcto. **Atomicidad estructural ADR-0001 demostrada con publicador real.**

### Conclusión

Progreso: la 5ª capa pasó de **"5 hipótesis pendientes"** a **"5/5 cerradas con evidencia (4 descartadas + 1 invalidada y reemplazada por causa raíz real)"**. **ADR-0001 sube a "validado estructuralmente — primer publicador real (`UserRegistered`) emite por Wolverine vía outbox transaccional + entrega RabbitMQ verificada E2E"**.

### Lecciones de Fase 1 + Fase 2

1. **Investigar la API real antes de implementar**. La verificación previa pre-Fase 2 (15 min read-only sobre XML docs) descartó 3 hipótesis antes de tocar código.
2. **La doc canónica importa más que la intuición**. La hipótesis "colisión de interceptors" se mantuvo viva por turnos sin evidencia que la sostuviera — corregida con investigación read-only de 20 min antes de declarar arquitectura. Wolverine simplemente no usa interceptors EF, y la doc lo deja claro al primer grep contra el XML.
3. **El smoke sintético out-of-handler de Fase 1 ya estaba mostrando el mismo bug** (`PublishAsync` sin flush) que el publicador real reveló en Fase 2. Fase 1 cerró con asterisco honesto en lugar de fabricar evidencia con experimentos que no aplicaban al uso real. Eso fue correcto.
4. **El Plan Mode obligatorio antes de cambiar interceptors** (mini-tarea cero) evitó romper auditoría/domain events sobre una hipótesis incorrecta. Patrón a repetir: detenerse a confirmar costos arquitectónicos antes de cualquier cambio que toque BuildingBlocks.

### Oportunidad revelada (semilla para Fase 3)

Wolverine 6.8 trae `M:Wolverine.EntityFrameworkCore.WolverineEntityCoreExtensions.PublishDomainEventsFromEntityFrameworkCore` — un **scraper nativo de domain events** que reemplaza funcionalmente al `DomainEventsInterceptor` propio. *"Tell Wolverine how to 'scrape' domain events from the active EF Core DbContext to publish as messages."* No bloqueante para Fase 2 (los 3 interceptors propios quedan intactos), pero es candidato a evaluar en Fase 3 si decidimos unificar el pipeline de domain events bajo Wolverine.

### Outgoing rule INV-9 — diferido post-Fase 3 (decisión Fase 3 Paso 3)

`IEnvelopeRule` para poblar `envelope.TenantId` automáticamente desde el Finbuckle
context en cada publicación outgoing se **difiere**.

**Causa**: `WolverineOptions.MetadataRules` se construye eager al momento del callback
`UseWolverine` — antes de que el `IServiceProvider` esté disponible. El patrón de
inyectar `IServiceProvider` al rule como workaround requiere `BuildServiceProvider()`
(anti-patrón que crea un container DI temporal) o `IWolverinePolicy.Apply()` con
orden DI ambiguo (la doc XML no clarifica si `Apply` corre pre- o post-DI build).

**Estado**: no bloqueante. Los tres publicadores migrados en Fase 2 (`UserRegistered`,
`TokenGenerated`, `FileFinalized`) propagan `TenantId` correctamente vía
`DeliveryOptions.TenantId` que el `IIntegrationEventPublisher<T>.PublishAsync` rellena
con `integrationEvent.TenantId` (que a su vez se captura del Finbuckle context en el
publish-site). El test E2E `WolverineUserRegisteredE2ETests` aserta
`sent.TenantId.ShouldBe(RootTenantId)` y pasa — propagación demostrada sin el rule.

El outgoing rule es **defensa adicional** para eventos derivados/cascadeados (publicación
dentro de un consumer Wolverine, sin tenant explícito en `DeliveryOptions`). Ese
escenario **no existe aún** en el código del proyecto.

**Investigar cuando llegue Fase 4**: si Wolverine 6.8 expone un hook `IWolverinePolicy`
post-DI-build que permita resolver servicios del container. Candidatos a evaluar:
`IMessageRoutePolicy`, `IEndpointPolicy`, o un hook tipo `OnHostStarted`. Si existe, la
implementación del rule queda trivial. Si no, la defensa cascadeada se cubre por
convención manual en cada handler Wolverine que necesite re-publicar.

### Chat + Notifications — pareja de Fase 3 (decisión Fase 2)

`SendMessageCommandHandler` (Chat) **no se migró en Fase 2** y queda diferido a Fase 3 junto con su consumer `MentionedInChannelIntegrationEventHandler` (Notifications). Justificación: el consumer **es producción real** — escribe `Notification` en `NotificationsDbContext` y dispara SignalR a `user:{id}` con `NotificationCreated` para que el bell-badge UI se actualice. Migrar el publisher a Wolverine sin migrar el consumer en el mismo paso rompería la entrega de notificaciones de mención hasta Fase 3 — pérdida observable. Separar publisher/consumer en commits distintos también introduce dependencia cruzada Chat → Notifications dentro de un solo cambio: si el consumer falla, el publisher queda incompleto sin rollback limpio. La pareja entra a Fase 3 junto con el middleware de tenant, que es donde naturalmente conviven publisher + consumer + tenant scope estructural.

**Hallazgo del patrón de Chat distinto a los publicadores ya migrados (UserRegistered, TokenGenerated)**: en `SendMessageCommandHandler`, `db.SaveChangesAsync(cancellationToken)` ocurre en la línea 97 **antes** del bloque de publish (líneas 105-128, un `foreach` sobre `notifyUserIds` que emite UN envelope por mención). Los dos publicadores anteriores tenían el SaveChanges al final o lo delegaban a servicios previos. Al migrar Chat en Fase 3, el orden tiene que reordenarse: o (a) los `PublishAsync` se ejecutan en el `foreach` y el `SaveChangesAndFlushAsync` final flushea todo el lote, lo cual requiere mover el `db.SaveChangesAsync` original a `SaveChangesAndFlushAsync`; o (b) el publisher se invoca después del flush separado del mensaje y los envelopes van en una segunda tx pequeña (no es el patrón canónico — rompe atomicidad estructural ADR-0001). El Plan Mode de Fase 3 para Chat debe abrir con esta decisión.

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
