# ADR-0005 — Routing por evento durante la migración a Wolverine

**Estado:** Aceptado · **Fecha:** 2026-06-14 · **Fase de adopción:** 2

## Contexto

ADR-0001 (outbox transaccional por módulo) y ADR-0004 (bus = Wolverine) decidieron migrar el bus de integration events del proyecto. Fase 1 cableó la infraestructura: setup del schema Wolverine en DbMigrator, enrollment EF del `IdentityDbContext`, codegen activo verificado.

La migración real (cambiar el bus efectivo de cada publicador y consumidor) es por fases acotadas. Mientras dure, conviven dos buses:

- **Bus propio (legacy):** `IEventBus` + `IOutboxStore` + `OutboxDispatcher` + RabbitMQ con exchange `maka.events`.
- **Wolverine (objetivo):** outbox EF + RabbitMQ con exchange por módulo (`maka.wolverine.identity.events`, `maka.wolverine.chat.events`, …).

Necesitamos un mecanismo para decidir, **por cada evento**, cuál bus lo entrega — sin recompilar y sin tocar el código del publicador o consumidor en cada toggle. Fases 2 y 3 dependen de poder migrar un evento a la vez y reverter si algo se rompe.

## Decisión

**Switch por evento + feature flag**. Configuración en `EventingOptions.IntegrationEventRouting` — diccionario `string → string` mapeando nombre del tipo del evento (`typeof(T).Name`) a `"Wolverine"` o `"Legacy"`. Default por omisión: `"Legacy"`.

El switch lo aplica una sola fachada: `IIntegrationEventPublisher<TDbContext>` (ver `BuildingBlocks/Eventing.Abstractions`). Cada módulo registra su instancia tipada vía `services.AddIntegrationEventPublisher<TDbContext>()`. Los publicadores inyectan `IIntegrationEventPublisher<TDbContext>` y llaman `PublishAsync(evt, ct)`. La fachada consulta el diccionario:

- `"Wolverine"` → `IDbContextOutbox<TDbContext>.PublishAsync(evt, new DeliveryOptions { TenantId = evt.TenantId })`. El envelope se enrola en el outbox EF y se persistirá en el próximo `SaveChangesAsync` del DbContext del scope (atomicidad estructural ADR-0001).
- cualquier otro valor → `IOutboxStore.AddAsync(evt, ct)` del bus propio.

Configuración inicial:

- `appsettings.json` (prod default): diccionario vacío → todos los eventos van por Legacy.
- `appsettings.Development.json`: `UserRegisteredIntegrationEvent = "Wolverine"` (Fase 2 — primer publicador migrado).
- Factory de tests: `UserRegisteredIntegrationEvent = "Wolverine"` para que el E2E valide la ruta nueva.

## Alternativas consideradas

- **Migración big-bang en un solo commit**: descartada. Sin escape hatch entre publicadores y consumidores; un error obliga a revertir el bundle entero.
- **Variable de entorno binaria global (`UseWolverine=true`)**: descartada. Granularidad equivocada — no permite migrar evento por evento.
- **Sub-clase / decorador por tipo**: descartada. Necesita conocer todos los tipos en compilación; configuración runtime es más simple y testeable.
- **Idempotencia cross-bus (dedupe por message-id)**: descartada — Wolverine no ofrece dedupe cross-bus, y publicar por ambos buses siempre genera envelopes con IDs distintos. El switch por evento garantiza una sola ruta por tipo.

## Consecuencias

**Positivas**
- Migración granular: un evento a la vez, con rollback inmediato cambiando el flag.
- Los publicadores no conocen el bus — call site limpio. Cuando Fase 5 borre el bus propio, la fachada se simplifica a una sola ruta sin tocar publicadores.
- El switch + el publisher genérico por `TDbContext` se alinean con la API canónica de Wolverine (`IDbContextOutbox<T>`).

**Negativas / costos**
- Una capa de indirección (`IIntegrationEventPublisher<T>`) en publicadores. Mitigada por la lectura sencilla del switch.
- Durante la migración, dos buses cableados consumiendo recursos (broker, hosted services). Fase 5 elimina el bus propio.
- El operador debe llevar registro de qué eventos están en Wolverine vs Legacy. Documentar en `wolverine-phase1-followups.md` el estado vigente por evento.

## Sitios donde se aplica

- **Fachada**: `src/BuildingBlocks/Eventing.Abstractions/IIntegrationEventPublisher.cs` (interfaz) + `src/BuildingBlocks/Eventing/IntegrationEventPublisher.cs` (impl con el switch).
- **Configuración**: `src/BuildingBlocks/Eventing/EventingOptions.cs` (campo `IntegrationEventRouting`).
- **Extensión DI**: `src/BuildingBlocks/Eventing/ServiceCollectionExtensions.cs` (`AddIntegrationEventPublisher<TDbContext>()`).
- **Identity (primer publicador migrado en Fase 2)**: `src/Modules/Identity/Modules.Identity/Services/UserRegistrationService.cs` (consume el publisher).
- **Routing transporte**: `src/Host/FSH.Starter.Api/Program.cs` (`opts.PublishMessage<T>().ToRabbitExchange(...)` por evento).
- **Defaults dev**: `src/Host/FSH.Starter.Api/appsettings.Development.json` (flag `Wolverine` para `UserRegisteredIntegrationEvent`).

## Cierre

ADR-0005 queda **Aceptado** al cierre de Fase 2. Las siguientes iteraciones migran los publicadores restantes (4 — uno por uno, con el mismo patrón) y luego los consumidores (Fase 3). Fase 5 elimina la fachada y deja solo la ruta Wolverine.
