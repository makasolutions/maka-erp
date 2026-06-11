# Eventing — domain events, integration events, Outbox/Inbox

Read before publishing/handling cross-module events. `src/BuildingBlocks/Eventing/`.

## Two tiers

- **Domain events** (in-process, pre-commit) — inherit `DomainEvent` (record: `EventId`, `OccurredOnUtc`, `CorrelationId`, `TenantId`). Raised on aggregates (`IHasDomainEvents`).
- **Integration events** (cross-module, async) — implement `IIntegrationEvent` (`Id`, `OccurredOnUtc`, `TenantId`, `CorrelationId`, `Source`). Handlers implement `IIntegrationEventHandler<T>` (single `HandleAsync(T, ct)`), are `sealed`, live in `Events/` or `IntegrationEventHandlers/`.

## The Outbox is the only way to publish

**Do not call `IEventBus` directly from a handler.** Publish via the outbox so it commits in the same transaction and survives crashes:

```csharp
await _outboxStore.AddAsync(integrationEvent, ct).ConfigureAwait(false);
```

`EfCoreOutboxStore.AddAsync` serializes + `SaveChanges` immediately. `OutboxDispatcherHostedService` polls every `OutboxDispatchIntervalSeconds` (default 10), `OutboxDispatcher` pulls a batch (`OutboxBatchSize`, default 100), publishes via `IEventBus`, and dead-letters after `OutboxMaxRetries` (default 5) → `IsDead`. `OutboxMessage`/`InboxMessage` are `IGlobalEntity` (no tenant filter — the dispatcher has no tenant context; `TenantId` is an explicit column).

## Idempotency is free (in-memory bus)

`InMemoryEventBus` resolves handlers in a fresh DI scope and applies the **Inbox**: skips if `IInboxStore.HasProcessedAsync(eventId, handlerName)`, marks processed after success. Composite key `{Id, HandlerName}`; concurrent-insert race is swallowed. Don't hand-roll dedup.

## Wiring (3 calls in the module's `ConfigureServices`)

```csharp
services.AddEventingCore(builder.Configuration);                        // serializer + bus + hosted dispatcher
services.AddEventingForDbContext<MyDbContext>();                        // outbox/inbox stores (scoped)
services.AddIntegrationEventHandlers(typeof(MyModule).Assembly);        // scans IIntegrationEventHandler<>
```

Bus = `EventingOptions.Provider`: `"RabbitMQ"` → `RabbitMqEventBus` (durable topic exchange); else `InMemoryEventBus` (default).

## Gotchas

- **Renaming/moving an integration event type breaks deserialization** — the outbox stores the assembly-qualified type name; `Type.GetType()` returns null → the message dead-letters. Keep event type names/namespaces stable, or migrate dead rows.
- In-memory bus runs handlers **synchronously in the caller's scope**. Since the Outbox-first fix
  (jun-2026), that caller is the **OutboxDispatcher pump**, not the HTTP request — handler
  exceptions trigger the outbox retry/dead-letter path, not a 500 to the user. Keep handler work
  minimal anyway (one dispatch loop processes the whole batch).
- Set `UseHostedServiceDispatcher=false` to drive the outbox via Hangfire instead of the hosted
  service. **Integration tests do exactly this** and also remove the hosted service — drain the
  outbox deterministically with `FshWebApplicationFactory.DispatchOutboxAsync()` after the action
  that publishes, before asserting on the consumer.

## ⚠️ PATRÓN OBLIGATORIO — tenant-scope en consumidores de integration events

Los consumidores corren en el pump del dispatcher **sin contexto HTTP/tenant**. Un consumidor que
resuelva un DbContext tenant-filtrado sin restaurar el tenant **falla en silencio** (Finbuckle
captura tenant null al CONSTRUIR el contexto → filtros NRE o writes mal estampados) — la peor
clase de falla multitenant: invisible y corruptora.

**Regla:** todo `IIntegrationEventHandler<T>` que toque un DbContext tenant-filtrado DEBE abrir un
**scope fresco**, instalar el `TenantId` del evento vía `IMultiTenantContextSetter`, y **solo
entonces** resolver el DbContext (inyectarlo por constructor NO sirve: ya capturó tenant null).

```csharp
using var scope = scopeFactory.CreateScope();
scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
    .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(
        new AppTenantInfo(@event.TenantId!, @event.TenantId!));
var db = scope.ServiceProvider.GetRequiredService<MyDbContext>();   // recién aquí
```

Implementación de referencia:
`src/Modules/Notifications/Modules.Notifications/IntegrationEventHandlers/MentionedInChannelIntegrationEventHandler.cs:63-74`.
Variante para lecturas multi-tenant (re-filtro explícito con `IgnoreQueryFilters`):
`WebhookFanoutHandler`. Eventos con `TenantId` null: rechazar/loggear, no escribir.

> La versión de **infraestructura** de este patrón (el dispatcher auto-restaura el tenant antes de
> invocar cualquier handler, imposible de olvidar) es decisión registrada en **ADR-0001**, a
> resolver junto con la topología del outbox ANTES del backbone (Inventory/Orders/Billing-DIAN).
