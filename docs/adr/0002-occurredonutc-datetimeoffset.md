# ADR-0002 · `OccurredOnUtc` canónico: `DateTimeOffset` en ambas jerarquías de eventos

**Estado: ACEPTADA** (aplicada jun-2026). | Relacionada: ADR-0001.

## Contexto

Las dos jerarquías de eventos divergían en tipo:
`DomainEvent.OccurredOnUtc` era `DateTimeOffset` (`Core/Domain/DomainEvent.cs:12`) pero
`IIntegrationEvent.OccurredOnUtc` era `DateTime` (`Eventing.Abstractions/IIntegrationEvent.cs:10`).
Convertir de domain → integration obligaba a descartar el offset; evidencia ejemplar del bug:
`UserRegisteredEventHandler.cs:33` hacía `notification.OccurredOnUtc.UtcDateTime`.

## Decisión

Tipo canónico **`DateTimeOffset`** para `OccurredOnUtc` en ambas jerarquías.

**Alcance (opción A, mínima):**
- Interfaz + los 4 integration events (Chat `MentionedInChannel`, Files `FileFinalized`,
  Identity `TokenGenerated`/`UserRegistered`) + sus 5 sitios constructores
  (`DateTimeOffset.UtcNow` / `TimeProvider.System.GetUtcNow()` sin `.UtcDateTime`).
- `RabbitMqEventBus.cs:82` se simplifica (`@event.OccurredOnUtc.ToUnixTimeSeconds()`).
- Las columnas de **bookkeeping** del outbox/inbox (`OutboxMessage.CreatedOnUtc/ProcessedOnUtc`,
  `InboxMessage.ProcessedOnUtc`) **siguen `DateTime` UTC**: el store asigna
  `@event.OccurredOnUtc.UtcDateTime` (`EfCoreOutboxStore.cs:39`). El offset completo viaja y se
  conserva donde importa: el **payload JSON**, que es la fuente de verdad de la rehidratación.
  → **Cero migraciones.**

## Compatibilidad

System.Text.Json round-tripea `DateTimeOffset` nativamente; las filas outbox ya persistidas
serializaron `DateTime.UtcNow` con sufijo `Z`, que deserializa a `DateTimeOffset` sin pérdida.
Filas pendientes/dead-letter previas al cambio siguen procesables.

## Rechazada

Opción B (columnas de bookkeeping también a `DateTimeOffset` + migración del schema identity):
sin beneficio real — son metadatos operativos siempre-UTC — y con migración de por medio.
