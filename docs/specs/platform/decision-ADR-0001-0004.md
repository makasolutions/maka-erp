# Medición ADR-0001/0004 — acoplamiento del bus de eventos

> **Tarea de medición read-only** (jun-2026, `develop` @ `3214a0ef`). Cero código modificado.
> Insumo para decidir entre **Opción 1** (outbox propio por-módulo a mano) y **Opción 2** (adoptar
> MassTransit v8). Toda cifra rastrea a archivo/tipo real.

## M1 · Consumidores (delgadez)

| Consumidor | LOC | LOC negocio | LOC transporte/infra | Veredicto |
|---|---|---|---|---|
| `TokenGeneratedLogHandler` | 39 | ~12 (log estructurado) | 0 | **DELGADO** |
| `UserRegisteredEmailHandler` | 50 | ~25 (compone + envía email) | 0 | **DELGADO** |
| `MentionedInChannelIntegrationEventHandler` | 95 | ~60 (crea notificación + push hub) | ~12 (CreateScope + `IMultiTenantContextSetter`, `:64-67`) | **MEDIO** |
| `WebhookFanoutHandler<TEvent>` | 122 | ~30 (lee suscripciones + fan-out) | ~50 (re-serializa con `IEventSerializer`, restaura tenant try/finally `:65-119`, `IgnoreQueryFilters` re-filtro, catch de retry sync) | **MEDIO-GRUESO** |

**Conclusión:** mayoría **DELGADOS/MEDIO**. Ninguno toca el transporte crudo (acks, routing keys, headers,
deserialización manual) — todos implementan `HandleAsync(@event)` limpio sobre el tipo del evento. La
"gordura" de los 2 más pesados es **exactamente el boilerplate de restauración de tenant** (`CreateScope` +
`IMultiTenantContextSetter`) que **ambas opciones eliminan**: ADR-0001 con auto-restore en el dispatcher,
MassTransit con un filtro de pipeline. Es decir, portar a `IConsumer<T>` es casi 1:1 y de paso **adelgaza**
los MEDIO. Único cuidado real: `WebhookFanoutHandler` es **open-generic** (`<TEvent>`) — el registro
equivalente en MassTransit (consumer genérico) necesita atención, no es bloqueante.

## M2 · Bus/dispatcher/inbox propios

| Componente | LOC |
|---|---|
| `RabbitMqEventBus` | 246 |
| `InMemoryEventBus` (dev) | 134 |
| `OutboxDispatcher` | 106 |
| `EfCoreOutboxStore` | 84 |
| `OutboxDispatcherHostedService` | 74 |
| `EfCoreInboxStore` | 60 |
| `JsonEventSerializer` | 36 |
| **Total núcleo eventing propio** | **~740 LOC** |

- **Features implementados:** retry de **publicación con delay fijo** (`PublishRetryDelayMs`,
  `RabbitMqEventBus.cs:68,115`) · retry de **dispatch por conteo** (`RetryCount++`, `OutboxDispatcher.cs:74-75`)
  · **dead-letter = flag** (`OutboxMessage.IsDead`, no exchange DLQ de RabbitMQ) · **inbox/idempotencia**
  `{eventId, handlerName}` (`InMemoryEventBus.cs:93-114`) · **topic exchange** estándar
  (`ExchangeDeclareAsync`, `:182`) · **tenant en header** al publicar (`["tenant-id"]=@event.TenantId`, `:89`).
- **Ausentes:** backoff exponencial · DLQ real (cola/exchange dead-letter) · prioridades · routing custom ·
  particionado por tenant · garantías de orden · redelivery.
- **Genérico (MassTransit lo da equivalente o mejor):** retry+backoff, DLQ/error/skipped queues, outbox
  EF, inbox idempotente, in-memory transport para tests, serialización. → **todo lo de arriba.**
- **A-la-medida que se perdería:** **nada bespoke.** Lo único "propio" es el `InMemoryEventBus` para dev,
  que MassTransit cubre con su **in-memory transport**.

**Conclusión:** ~740 LOC de **plomería genérica** (de hecho, una versión más pobre: retry de delay fijo y
"dead-letter" de flag, sin DLQ real ni backoff). No hay comportamiento propio valioso que mantener pese a Opción 2.

## M3 · DbContext y publicadores

- **DbContext de módulo: 13** (snapshots de migración): Audit, Billing, Catalog, Chat, Files, Hr, Identity,
  Lookups, Notifications, Parties, Tenant, Tickets, Webhook.
- **Emiten eventos HOY: 1** (Identity — único `DbSet<OutboxMessage>`).
- **Emitirían a futuro:** prácticamente todo módulo transaccional — **Inventory, Orders, Billing-DIAN,
  Catalog** (cuando reaccione), Parties, Purchasing, Warranties… → la topología actual (1 tabla outbox en
  Identity) **no escala** sin atomicidad por-módulo (ADR-0001).
- **5 publicadores — mecanicidad del recableo `IOutboxStore.AddAsync(...)` → `IPublishEndpoint.Publish(...)`:**
  | Publicador | Call actual | Recableo |
  |---|---|---|
  | `Chat/SendMessageCommandHandler.cs:112` | `await outbox.AddAsync(new MentionedInChannel…(…))` | trivial 1:1 |
  | `Files/FinalizeUploadCommandHandler.cs:87` | `await outbox.AddAsync(new FileFinalized…(…))` | trivial 1:1 |
  | `Identity/UserRegisteredEventHandler.cs:42` | `await outboxStore.AddAsync(integrationEvent…)` | trivial 1:1 |
  | `Identity/GenerateTokenCommandHandler.cs:141` | `await _outboxStore.AddAsync(integrationEvent…)` | trivial 1:1 |
  | `Identity/UserRegistrationService.cs:278` | `await outboxStore.AddAsync(integrationEvent…)` | trivial 1:1 |

  Los 5 son `AddAsync(evento)` puro → cambio mecánico. Opción 2 además los hace **transaccionales por
  módulo** vía el outbox EF de MassTransit (resuelve ADR-0001 de paso).

## M4 · Contención RabbitMQ

- Archivos que referencian `RabbitMQ.Client` **fuera** de la clase del bus: **0**.
- `using RabbitMQ.Client` aparece en **exactamente 1 archivo**: `RabbitMq/RabbitMqEventBus.cs`.
- → **ENCAPSULADO** por completo. Ningún módulo de negocio toca el transporte. Swap a MassTransit = limpio.

## Extra

- **Tests directos del bus/outbox/dispatcher: 0.** Lo único relacionado: `EventingArchitectureTests` (regla
  de arquitectura, no comportamiento), `GenerateTokenCommandHandlerTests` (testea el handler, no el store),
  `FshWebApplicationFactory.DispatchOutboxAsync` (helper de drenaje). El núcleo (retry→DLQ, inbox dedupe,
  publish) **no tiene cobertura directa** → migrar a infra probada **mejora** la testabilidad neta.
- **Tenant en headers: SÍ al publicar** (`["tenant-id"]=@event.TenantId`, `RabbitMqEventBus.cs:89`), **pero
  el consumidor lo lee del payload** (`@event.TenantId`), no del header → el header está puesto pero **no se
  consume** hoy (redundante). MassTransit lo manejaría vía `MessageHeaders`/filtro de forma estándar.

## Auto-veredicto

Según la rúbrica:
- **Consumidores:** mayoría DELGADOS/MEDIO, sin acoplamiento a transporte; la "gordura" es boilerplate de
  tenant-restore que ambas opciones borran → **a favor de Opción 2**.
- **Bus/dispatcher/inbox:** ~740 LOC de **plomería genérica** (más pobre que el estándar), **0 bespoke** →
  **a favor de Opción 2**.
- **RabbitMQ:** **contenido en 1 archivo** → **a favor de Opción 2**.

**Las 3 condiciones de la rúbrica para Opción 2 se cumplen.**

> **Inclinación: Opción 2 — adoptar MassTransit v8 (Apache 2.0).** Reemplaza ~740 LOC de plomería propia
> por infra probada (outbox **transaccional por módulo** → resuelve ADR-0001; retry+backoff y DLQ reales;
> in-memory transport para tests; filtro de pipeline que **centraliza el tenant-restore** → resuelve la
> otra mitad de ADR-0001), con recableo **mecánico** de 5 publicadores y porte **casi 1:1** de 4
> consumidores. **Resuelve ADR-0001 y ADR-0004 de una vez.**
>
> **Variable que inclina la balanza:** **M4 (RabbitMQ contenido en 1 archivo) + M2 (cero bespoke)** — el
> swap es barato y no se pierde nada. **Riesgo único a planear (no bloqueante):** el `WebhookFanoutHandler`
> open-generic y el cambio del harness de tests de `InMemoryEventBus` → in-memory transport de MassTransit.
>
> *No es decisión final: es la inclinación basada en evidencia. La decide el humano.*
