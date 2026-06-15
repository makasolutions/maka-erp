# Medición migración a Wolverine — resuelve ADR-0001 + ADR-0004

> Read-only, jun-2026 (`develop` @ `3214a0ef`). **Cero código modificado.** Wolverine entra como capa de
> mensajería + outbox/inbox transaccional + bus; **NO** reemplaza a `Mediator` source-gen (in-process).

---

## M1 · Compatibilidad — **OK** ✅

| Tema | Repo | Wolverine 6.8.0 | Veredicto |
|---|---|---|---|
| TFM | `net10.0` (`src/Directory.Build.props`) | net9.0 + **net10.0** | ✅ |
| EF Core | **10.0.8** (`Directory.Packages.props`) | EF Core ≥ **10.0.2** | ✅ |
| RabbitMQ.Client | **7.2.1** | ≥ **7.1.2** | ✅ |
| Mediator source-gen | 3.0.2 (independiente) | Wolverine se limita a mensajería | ✅ |

**Paquetes objetivo:** `WolverineFx 6.8.0`, `WolverineFx.RabbitMQ 6.8.0`, `WolverineFx.EntityFrameworkCore 6.8.0`
(MIT). Gate **abierto**.

---

## M2 · Consumidores → handlers Wolverine

Wolverine descubre handlers por **convención** (clase con método `public Task Handle(TEvent evt, …)` o
`HandleAsync`). Sin interfaces marcadoras. Las dependencias del método llegan por inyección de parámetro
(scope creado por Wolverine).

| Consumidor (hoy) | Mapeo Wolverine | Tenant-restore actual | Veredicto |
|---|---|---|---|
| `TokenGeneratedLogHandler` (39 LOC) | `Handle(TokenGeneratedIntegrationEvent evt, ILogger)` | 0 LOC | **TRIVIAL** |
| `UserRegisteredEmailHandler` (50 LOC) | `Handle(UserRegisteredIntegrationEvent evt, IEmailService)` | 0 LOC | **TRIVIAL** |
| `MentionedInChannelIntegrationEventHandler` (95 LOC) | `Handle(MentionedInChannelIntegrationEvent evt, NotificationsDbContext db, IHubContext<AppHub>)` — el DbContext lo enlista Wolverine; el `IServiceScopeFactory + CreateScope + IMultiTenantContextSetter` (~12 LOC) **desaparece** vía middleware de tenant (M6). | ~12 LOC | **TRIVIAL** (queda 1:1 sobre el negocio puro) |
| `WebhookFanoutHandler<TEvent> : IIntegrationEventHandler<TEvent>` (122 LOC) | **🔴 RIESGO PRINCIPAL** — Wolverine **no resuelve handlers open-generic** por convención. Modelado actual: `services.AddIntegrationEventHandlers(typeof(IIntegrationEventHandler<>), typeof(WebhookFanoutHandler<>))` (`WebhooksModule.cs:43-45`) → el bus cierra el tipo en runtime. | ~50 LOC (`try/finally` con `IMultiTenantContextSetter`, re-serializa con `IEventSerializer`, re-filtro `IgnoreQueryFilters`) | **REQUIERE-DISEÑO** |

**`WebhookFanoutHandler` — 3 opciones (sin implementar):**
1. **Catch-all subscription en el shard inbound** (recomendada): registrar **una sola** suscripción al
   exchange RabbitMQ que entrega TODO mensaje a un handler concreto `WebhookFanoutMessageHandler` que
   recibe `Envelope` (API de Wolverine) y enruta a `IWebhookDispatcher` por `MessageType`. Costo bajo,
   semántica idéntica, sigue siendo open a futuros eventos sin tocar Webhooks.
2. **Registro explícito por tipo concreto:** un handler por cada `IIntegrationEvent` conocido. Sencillo
   pero rompe la doctrina ("Webhooks reacciona a CUALQUIER evento") — cada evento nuevo obliga a tocar
   Webhooks.
3. **Source-generator propio** que materialice handlers cerrados para cada `IIntegrationEvent` del repo
   en tiempo de build. Caro y específico — no compensa.

Recomendación: **Opción 1**. Riesgo medible, no bloqueante.

---

## M3 · Outbox / DbContext (13 contextos)

**Patrón Wolverine:**
```csharp
opts.PersistMessagesWithEntityFrameworkCore<TDbContext>();   // por DbContext
// y en el handler:
public async Task Handle(Cmd cmd, NotificationsDbContext db, IMessageBus bus) {
    db.Notifications.Add(n);
    await bus.PublishAsync(new MentionedInChannelIntegrationEvent(…));  // se enlista en el SaveChanges
    // Wolverine commitea outbox + dominio en UN SaveChanges → atómico
}
```
- **Esquema:** Wolverine crea sus tablas envelope (`wolverine_outgoing_envelopes`,
  `wolverine_incoming_envelopes`, `wolverine_dead_letters`, `wolverine_nodes`) — **una vez por DbContext**.
  Configurables por `schema` ⇒ se puede usar el schema del módulo (`catalog`, `chat`, …). Migración: un
  `dotnet ef migrations add Wolverine_Envelopes` por DbContext que necesite emitir/consumir, vía el
  mismo proyecto `Migrations.PostgreSQL` (carpeta del módulo).
- **Hoy emite 1** (Identity). **A futuro N:** se enrola **cuando el módulo lo necesita**, no a-la-vez.
- **Patrón mínimo para Inventory greenfield:**
  1. `InventoryModule.ConfigureServices` registra el módulo en Wolverine via `WolverineDbContextPolicy<InventoryDbContext>()`.
  2. Una migración Wolverine_Envelopes en el schema `inventory`.
  3. Listo. **No toca a los demás módulos** — cada DbContext entra por su cuenta.

---

## M4 · Convivencia con Mediator source-gen

- **Sin choque de descubrimiento:** Mediator source-gen genera el `IMediator` y handlers `ICommand/IQuery`
  en compile-time (assemblies declarados, `Program.cs:34-37`). Wolverine descubre clases con método `Handle`
  por reflexión en sus propios assemblies declarados (`AddAssembly(...)`/escaneo). Son **dos universos
  separados** — ningún handler implementa ambas firmas.
- **Limitación a mensajería:** se logra **no llamando** `IMessageBus.InvokeAsync` desde código in-process
  (Mediator sigue siendo `IMediator.Send`). Wolverine se queda con `PublishAsync` para integration events.
  Programa: el ADR-0004 declara explícitamente la convivencia; los endpoints siguen llamando `mediator.Send`,
  los handlers siguen siendo `ICommandHandler<,>`. Wolverine **solo** entra en: (a) endpoints/handlers que
  emiten integration events, (b) consumidores de integration events.
- **Riesgo concreto si NO se limita:** dos buses de comandos → confusión, doble registro, posible
  doble-ejecución de handlers que implementen ambos contratos. Mitigación: **no registrar** handlers de
  comando en los assemblies que Wolverine escanea (`AddAssembly` solo los assemblies con consumidores de
  eventos: Notifications, Webhooks, Identity/Events, Chat/Events).

Veredicto: **convivencia LIMPIA** con disciplina explícita del scan.

---

## M5 · Publicadores (5)

| Sitio actual | Equivalente Wolverine | Mecanicidad |
|---|---|---|
| `Chat/SendMessageCommandHandler.cs:112` `await outbox.AddAsync(new MentionedInChannel…(…))` | `await bus.PublishAsync(new MentionedInChannel…(…))` (DbContext enrolado) | trivial 1:1 |
| `Files/FinalizeUploadCommandHandler.cs:87` | idem | trivial 1:1 |
| `Identity/UserRegisteredEventHandler.cs:42` | idem | trivial 1:1 |
| `Identity/GenerateTokenCommandHandler.cs:141` | idem | trivial 1:1 |
| `Identity/UserRegistrationService.cs:278` | idem | trivial 1:1 |

Inyectar `IMessageBus` en lugar de `IOutboxStore`. El `SaveChanges` del módulo persiste también el
envelope outgoing → **atomicidad real por módulo** (cierre estructural de ADR-0001).

---

## M6 · Tenant (vuelve estructural)

Hoy: header `tenant-id` se **escribe** al publicar (`RabbitMqEventBus.cs:89`) pero el consumidor lo lee del
**payload** (`@event.TenantId`) → header redundante.

Con Wolverine:
- **Outgoing middleware** lee el tenant ambiente y lo escribe en `envelope.TenantId` (campo nativo).
- **Incoming middleware** (`IWolverineHandler` policy / `IMessageEnricher`) toma `envelope.TenantId` y
  hace `IMultiTenantContextSetter.MultiTenantContext = new(...)` **antes** de invocar el handler.
- Resultado: los 12 LOC de `MentionedInChannel…Handler` (`CreateScope + setter`) y los 50 LOC del `try/finally` de
  `WebhookFanoutHandler` **desaparecen**; INV-9 deja de ser "regla por convención" y pasa a ser
  **estructural** — ningún handler nuevo puede olvidarla. (Esta es la otra mitad de ADR-0001.)

---

## M7 · RabbitMQ

| Hoy (`RabbitMqEventBus.cs`) | Wolverine |
|---|---|
| Topic exchange durable, `_options.ExchangeName` (`:181-183`) | `opts.UseRabbitMq().DeclareExchange(name, ExchangeType.Topic, durable: true)` — equivalente |
| `routingKey = eventType.FullName ?? eventType.Name` (`:61`) | Routing convencional por tipo (`SendMessage<T>().ToRabbitExchange(...)`) — equivalente |
| Reintento publicación con delay fijo (`PublishRetryDelayMs`) | Retry policy nativa con backoff exponencial — **mejora** |
| Dead-letter = flag `IsDead` en la fila outbox | Cola/exchange DLQ real de RabbitMQ — **mejora** |
| Tenant header (`tenant-id`) redundante | `envelope.TenantId` + middleware → estructural — **mejora** |
| `IEventSerializer` propio (JSON 36 LOC) | Serializer nativo Wolverine (System.Text.Json) — **simplifica** |

**Conserva** los nombres del exchange (config) si se mantiene el mismo. **Cambia** el formato del envelope
(Wolverine usa su propio frame). **Pierde:** nada bespoke. La transición de exchanges/colas es coordinable
sin downtime con dual-publish corto (no necesario si el corte es en dev primero).

---

## M8 · Inbox / idempotencia

Hoy: `EfCoreInboxStore` (60 LOC) por clave `{eventId, handlerName}` en `InboxMessage` (Identity schema).
Wolverine: **Inbox nativo** en `wolverine_incoming_envelopes` con `Id` del envelope + handler. Habilitado con
`DurableInbox()` al declarar el listener. **Misma semántica** de "skip si ya procesado". Migración:
descartar `EfCoreInboxStore`+`InboxMessage`; Wolverine crea la tabla en cada DbContext consumidor.

---

## M9 · Blast radius BuildingBlocks

**Borrar (todo `src/BuildingBlocks/Eventing/` excepto Abstractions):**
```
Eventing/EventingOptions.cs
Eventing/ServiceCollectionExtensions.cs
Eventing/Serialization/JsonEventSerializer.cs
Eventing/Outbox/EfCoreOutboxStore.cs · IOutboxStore.cs · OutboxMessage.cs ·
  OutboxDispatcher.cs · OutboxDispatcherHostedService.cs
Eventing/Inbox/EfCoreInboxStore.cs · IInboxStore.cs · InboxMessage.cs
Eventing/InMemory/InMemoryEventBus.cs
Eventing/RabbitMq/RabbitMqEventBus.cs · RabbitMqOptions.cs
```
≈ **~880 LOC de BuildingBlocks borradas**.

**Conservar (Abstractions):**
```
Eventing.Abstractions/IIntegrationEvent.cs          ← `OccurredOnUtc : DateTimeOffset` (ADR-0002)
Eventing.Abstractions/IIntegrationEventHandler.cs   ← se mantiene como tipo marcador OPCIONAL
                                                       (los handlers Wolverine son por convención,
                                                        pero la interfaz puede convivir si se quiere
                                                        documentar; o eliminarse — ver §riesgos)
```

**Adaptar:**
- `BuildingBlocks/Eventing.Abstractions/IEventBus.cs` — **eliminar** (Wolverine usa `IMessageBus`).
  Tras esto, `EventingArchitectureTests` deja de protegerse del **tipo** `IEventBus` propio y empieza a
  proteger del **tipo Wolverine** (ajuste de la regla, mismo espíritu INV-8 blindado).

**Architecture.Tests a tocar:**
- `EventingArchitectureTests.cs`: cambiar el `EventBusTypeFullName = "FSH.Framework.Eventing.Abstractions.IEventBus"`
  por la regla equivalente sobre Wolverine. Forma sugerida: ningún tipo de módulo puede usar
  `IMessageBus.PublishAsync` **fuera** de un handler que tenga un DbContext enlistado (regla "publicación
  enrolada" = sustituta de "Outbox-first"). El control positivo (consumidores legítimos) se actualiza a
  "implementa convención de Handle Wolverine".
- `TenantIsolationTests.cs`: sin cambios funcionales — sigue ejerciendo el filtro de tenant; gana un
  hermano que pruebe que el middleware de tenant se aplica.

**Invariantes:**
- **INV-8** (Outbox-first) → de "regla violable" a "estructural": no se puede publicar fuera del outbox
  porque `bus.PublishAsync` siempre persiste envelope si el DbContext está enlistado.
- **INV-9** (tenant restaurado) → de "convención por consumidor" a **estructural** via middleware.
- **INV-3** (append-only ledgers) → se preserva: `wolverine_outgoing_envelopes` es append-and-mark.

---

## M10 · Tests

- **Mover:** `FshWebApplicationFactory.DispatchOutboxAsync()` (helper de drenaje propio) → `IHost.WaitForMessageToBeReceivedAsync<T>()` / **TrackedSessions** de Wolverine (esperan determinísticamente a que el outbox drene y el handler termine). API más limpia, sin race.
- **Mover:** `InMemoryEventBus`-based assertions → **in-memory transport de Wolverine** (no tabla, todo en proceso). Tests más rápidos.
- **Borrar:** `EventingArchitectureTests.SyntheticEventBusViolation` se reescribe contra el nuevo símbolo.
- **Cobertura NUEVA (paga la deuda M2 del baseline):**
  - **Atomicidad por módulo:** crear evento + abortar SaveChanges → outbox vacío (test directo).
  - **Tenant middleware:** publicar bajo tenant A, asertar que el consumidor escribe bajo A; cambiar el header → fuga rechazada.
  - **DLQ real:** N reintentos → cola dead-letter (no flag).
  - **Inbox idempotencia:** doble entrega → 1 procesamiento.

---

## Plan de migración por fases (con blast radius)

| Fase | Qué | Blast radius | Desbloquea Inventory? |
|---|---|---|---|
| **1 — Wiring Wolverine (Identity)** | Añadir 3 paquetes `WolverineFx*`; `UseWolverine` en Host con `AddAssembly(Identity)`; `PersistMessagesWithEntityFrameworkCore<IdentityDbContext>`; migración Wolverine_Envelopes en schema identity; declarar middleware de tenant outgoing/incoming. **Outbox propio sigue vivo en paralelo.** | Host + `IdentityModule` (3 archivos). Sin recablear ningún publicador/consumidor. 51 arch-tests verdes (regla actual sigue protegiendo el `IEventBus` propio). | No |
| **2 — Recablear 5 publicadores** | Cambiar `IOutboxStore.AddAsync` → `IMessageBus.PublishAsync` en los 5 sitios; `Files/Chat/Notifications` enrolan su DbContext en Wolverine (3 migraciones Wolverine_Envelopes nuevas). | 5 handlers + 3 módulos (`Chat/Files/Notifications`). Integration tests de menciones se reescriben con TrackedSessions. | No |
| **3 — Portar 4 consumidores** | `TokenGeneratedLog`/`UserRegisteredEmail` → handlers Wolverine triviales. `MentionedInChannel…` → handler limpio (sin scope/setter manual). `WebhookFanoutHandler` → **catch-all sobre `Envelope`** (decisión M2 op.1). Middleware de tenant cierra INV-9. | 4 archivos + 1 archivo nuevo en Webhooks. Architecture.Tests: `EventingArchitectureTests` se actualiza al nuevo símbolo (no se borra). | No |
| **4 — Tests** | Mover los integration tests al harness Wolverine; añadir los 4 tests de cobertura nueva (atomicidad/tenant/DLQ/inbox). | `Tests/Integration.Tests/Infrastructure` + suite nueva. Sin cambios de producción. | No |
| **5 — Retirar bus propio** | Borrar 14 archivos `BuildingBlocks/Eventing/{!Abstractions}`, ~880 LOC. Borrar `IEventBus` de `Eventing.Abstractions`. Actualizar nota de constitución + ADR-0001 cerrado + ADR-0004 cerrado. | BuildingBlocks (protegido). Última fase con riesgo de regresión — todos los tests deben quedar verdes antes. | **Sí — desbloquea Inventory en cuanto Fase 5 cierra.** *(En estricto rigor Inventory puede arrancar tras Fase 1, pero conviene esperar a Fase 3 para heredar el middleware de tenant; Fase 5 es la limpieza final.)* |

**Convivencia durante Fases 1–4:** el bus propio sigue operando para módulos no migrados; Wolverine corre
en paralelo en los migrados. Cero downtime de funcionalidad.

---

## Riesgos priorizados

1. **🔴 `WebhookFanoutHandler<TEvent>` open-generic** (M2). Mitigación: catch-all sobre `Envelope` (1
   archivo nuevo). **Bloquea Fase 3 si no se diseña antes.** Cubierto, no bloqueante con la opción 1.
2. **🟠 Convivencia Mediator/Wolverine** (M4). Mitigación: disciplina explícita del `AddAssembly` de
   Wolverine (solo assemblies con consumidores de eventos) + nota en `eventing.md` + arch-test sustituto
   que prohíba mezclar firmas. Riesgo de doble bus si no.
3. **🟠 Tabla `wolverine_*` por DbContext × N módulos.** Cada módulo que emita/consuma necesita su
   migración. Mitigación: hacerla parte del checklist de `add-module` y de cada fase del plan.
4. **🟡 Cambio del formato de envelope RabbitMQ** (M7). En dev es invisible; en prod requiere drenar la
   cola pre-corte. No crítico antes del backbone.
5. **🟡 Cobertura cero del bus actual** (M10) — la migración paga la deuda, pero **durante** la
   transición no hay red de seguridad para regresiones. Mitigación: añadir 2 tests de fuego al cierre de
   Fase 2 (publish atómico + tenant correcto) antes de seguir.
6. **🟢 Deuda menor**: `IIntegrationEventHandler<>` queda como interfaz marcador opcional. Decidir si se
   elimina (Wolverine no la necesita) o se conserva como documentación. No bloquea nada.

---

*Decisión que cierra ambos ADRs:* este plan resuelve **ADR-0001** (atomicidad por DbContext + tenant
estructural) y **ADR-0004** (bus) **a la vez**. Inventory puede iniciar tras Fase 3 (heredando el
middleware de tenant) o esperar a Fase 5 (limpieza completa). No se vuelve a este tema hasta cerrar Fase 5.
