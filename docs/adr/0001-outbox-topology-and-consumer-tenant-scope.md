# ADR-0001 · Topología del Outbox + tenant-scope de consumidores

**Estado: ABIERTA — BLOQUEANTE pre-backbone.** Resolver **antes** de implementar
Inventory / Orders / Billing-DIAN. | Fecha: jun-2026 | Contexto: fix de fundación "malla de eventos".

## Contexto

La doctrina (`.agents/rules/eventing.md`) es Outbox-first: ningún integration event se publica
con `IEventBus` directo. El fix de jun-2026 (commit `2162f6c7`) migró los 3 publicadores
violadores (Chat, Files, Identity) a `IOutboxStore.AddAsync`.

Al hacerlo se midió la infraestructura real:

- El Outbox del starter es **central y mono-DbContext**: la única tabla `OutboxMessage` vive en
  `IdentityDbContext` (único `DbSet<OutboxMessage>` del repo); `IOutboxStore` se registra UNA vez
  (`Eventing/ServiceCollectionExtensions.cs:62`, `AddScoped` sin key → `EfCoreOutboxStore<IdentityDbContext>`);
  un solo `OutboxDispatcher` lee de ese store.
- `EfCoreOutboxStore.AddAsync` hace su **propio** `SaveChanges` (`EfCoreOutboxStore.cs:48-49`).
  La atomicidad real mutación+evento existe **solo** cuando ambos comparten DbContext (caso
  Identity). Para Chat/Files el resultado es dual-write **durable pero no atómico**: si el
  proceso muere entre el `SaveChanges` del módulo y el del outbox, queda mutación sin evento
  (o, en el orden inverso, evento huérfano).

## Decisión pendiente (la pregunta de fundación)

**Outbox por módulo (atómico) vs outbox central (actual, no atómico para módulos que no
comparten DbContext).**

| | Central (actual) | Por módulo |
|---|---|---|
| Atomicidad | Solo Identity | Total (la fila outbox entra en el MISMO `SaveChanges`/transacción del módulo) |
| Costo | 0 | Cambio de BuildingBlocks: stores tipados/keyed por DbContext, dispatcher multi-contexto, `DbSet<OutboxMessage>` + migración por módulo |
| Riesgo si se queda central | Chat/Files: trivial (una mención perdida). **Backbone de comercio: inaceptable** — un `OrderPlaced` huérfano o perdido descuadra inventario y contabilidad | — |

**Tradeoff registrado:** para los módulos colaborativos actuales el dual-write no atómico es
tolerable; para Orders/Inventory/Billing-DIAN la atomicidad **no es opcional**. De ahí el
carácter bloqueante.

## Decisión hermana (mismo paquete pre-backbone): tenant-scope automático del consumidor

Bajo Outbox, los consumidores corren en el pump del dispatcher **sin contexto HTTP/tenant**.
Un consumidor que resuelva un DbContext tenant-filtrado sin restaurar el tenant **falla en
silencio** (Finbuckle captura tenant null al construir el contexto → filtros NRE o writes
mal estampados) — en multitenant, la peor clase de falla: invisible y corruptora.

- **Patrón vigente (manual, documentado en `eventing.md`):** scope fresco + instalar el
  `TenantId` del evento vía `IMultiTenantContextSetter` ANTES de resolver el DbContext.
  Implementación de referencia:
  `src/Modules/Notifications/IntegrationEventHandlers/MentionedInChannelIntegrationEventHandler.cs:63-74`.
- **Decisión a tomar (infraestructura):** mover el patrón a la base — el dispatcher (o un
  decorator/base del consumidor) **auto-restaura** el tenant desde `IIntegrationEvent.TenantId`
  antes de invocar cualquier handler, para que ningún consumidor pueda olvidarlo. Mismo
  principio que el Architecture.Test del Outbox: convertir una regla violable en una imposible
  de violar. **No implementada aún** — se resuelve junto con la topología, antes del backbone.

## Compatibilidad hacia adelante

Los diffs del fix (`2162f6c7`) son **forward-compatible**: los handlers llaman
`IOutboxStore.AddAsync` agnósticos del cableado del store. Migrar a outbox-por-módulo NO
requiere rehacerlos.

## Footguns documentados (vigentes mientras la decisión esté abierta)

1. **Ningún módulo debe llamar `AddEventingForDbContext<T>`** — el registro es last-wins y
   pisaría el store global de Identity (y su tabla ni existiría en el otro schema).
2. La evolución a per-module es **cambio de BuildingBlocks** (protegido): task aparte con su
   propio blast radius medido.
