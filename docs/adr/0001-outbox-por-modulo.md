# ADR-0001 · Outbox transaccional por módulo (resuelto por Wolverine)

- **Estado:** Aceptado · junio 2026
- **Supersede:** 0001-outbox-topology-and-consumer-tenant-scope.md (estado anterior: Propuesta, 11-jun-2026).
- **Decisores:** Juan (CEO/sistemas) + asistente de planeación
- **Ligado a:** ADR-0002 (DateTimeOffset) · **ADR-0004** (bus de eventos — resuelto en conjunto)
- **Evidencia:** `docs/specs/platform/SPEC.md` (as-built) · `docs/specs/platform/decision-ADR-0001-0004.md` (medición del bus) · `docs/specs/platform/migration-wolverine.md` (medición de migración)

---

## Contexto

El as-built de la plataforma reveló que el outbox actual es **central mono-DbContext**: una sola tabla `OutboxMessage` cuyo único `DbSet` vive en `IdentityDbContext`. `IOutboxStore.AddAsync` hace su propio `SaveChanges`. Consecuencia: el cambio de dominio y el evento se persisten en la **misma transacción solo cuando comparten DbContext**, lo cual hoy ocurre únicamente en Identity. Para cualquier otro módulo (Inventory, Orders, Billing-DIAN, Catalog) son **dos `SaveChanges` separados = dual-write**: si el proceso muere entre el primero y el segundo, o se pierde el evento, o el evento queda emitido sobre un cambio revertido.

En Inventario, donde un movimiento sin su asiento contable corrompe los libros, esto es inaceptable. **Compuerta dura antes del backbone de comercio.**

Además, la restauración del tenant al consumir eventos depende hoy de que cada consumidor lo haga a mano (convención). INV-9 es "regla escrita", no "regla imposible de violar".

## Decisión

**Adoptar outbox transaccional por módulo y middleware estructural de tenant**, implementado mediante **Wolverine v3 (MIT)** (ver ADR-0004). El envelope del mensaje se persiste **en el mismo `SaveChanges` del DbContext del módulo** que originó el cambio — atomicidad por construcción, no por disciplina. Un middleware del pipeline de Wolverine escribe `envelope.TenantId` al publicar y lo restaura en `IMultiTenantContextSetter` antes del handler al consumir — INV-9 estructural.

## Opciones consideradas

### Opción 1 — Outbox propio por-módulo, a mano
Hacer que `IOutboxStore` participe del `DbContext` ambiente del handler para que `AddAsync` + cambio de dominio commiteen en un solo `SaveChanges`; tabla `OutboxMessage` por módulo; dispatcher barriendo N tablas; auto-restore de tenant en el dispatcher; mantener el bus propio (`RabbitMqEventBus`). Cambio concentrado en BuildingBlocks + 1 migración por módulo.

- **Pros:** menor superficie tocada ahora; cero dependencia nueva; control total.
- **Contras:** se construye la capa más crítica del sistema (atomicidad) sin red de tests (cobertura actual = 0); ~740 LOC propias de eventing siguen mantenidas y testeadas por nosotros para siempre; tenant-restore sigue siendo disciplina, no estructura, salvo que también se mueva al dispatcher.

### Opción 2 — MassTransit v8/v9
**Descartada antes de evaluarse a fondo** por licencia y horizonte: MassTransit v8 entra en end-of-life a fin de 2026; v9 es comercial (mínimo USD 400/mes con umbral sub-1M ambiguo). Cimiento incompatible con un SaaS multi-tenant que debe durar años.

### Opción 3 — Wolverine v3 (MIT) — *Elegida*
Adoptar Wolverine como capa de mensajería + outbox transaccional + bus, **conservando Mediator source-gen** para in-process. La medición confirmó: `opts.PersistMessagesWithEntityFrameworkCore<TDbContext>()` enrola el DbContext en el message context y persiste el envelope en el mismo `SaveChanges` → **atomicidad estructural por módulo** sin código propio. Middleware outgoing/incoming centraliza el tenant.

- **Pros:** atomicidad por construcción, no por convención; tenant estructural; ~880 LOC propias eliminadas; outbox transaccional nativo con soporte EF Core, escrito por el autor de Marten; harness de tests determinista (TrackedSessions + in-memory transport); MIT, sin EOL ni costo.
- **Contras:** dependencia nueva; migración real (recablear 5 publicadores + 4 consumidores); el `WebhookFanoutHandler<TEvent>` open-generic requiere rediseño puntual (mitigación lista — ver M2).
- **Riesgo del estado final:** bajo (infra probada, mejor testabilidad). **Riesgo durante la migración:** medio, mitigado por el plan en 5 fases.

## Validación upstream (FSH / Mukesh)

- El eventing/outbox actual del repo es **divergencia local** — no existe en el release principal del starter. El PR #1152 introduce un eventing distinto (in-memory + Hangfire dispatcher), no comparable.
- Mukesh evaluó Wolverine en issue #894 (oct-2023) y discusión #870 (.NET 8 migration); decidió **quedarse con Mediator source-gen** y no adoptar Wolverine.
- **Divergencia consciente:** adoptamos Wolverine **solo como capa de mensajería/outbox**, conservando Mediator source-gen para in-process. El razonamiento de Mukesh ("OSS estándar, sin frameworks de los que no puedas salir") se respeta en lo que sí coincidimos.

## Consecuencias

**Estructurales:**
- INV-8 (eventos por outbox, no por tablas compartidas) pasa de Architecture.Test sobre `IEventBus` a regla "publicación enrolada en DbContext" sobre `IMessageBus`.
- INV-9 (tenant) pasa de **convención** a **estructural** (middleware).
- ADR-0002 sigue vigente (`DateTimeOffset` se mapea al envelope de Wolverine).

**Operativas:**
- Se borran ~880 LOC (14 archivos de `Eventing/*` + `IEventBus.cs`); se conserva `IIntegrationEvent.cs`.
- `EventingArchitectureTests` se reescribe (mismo espíritu sobre el nuevo modelo).
- Cada módulo que emite eventos gana una migración `Wolverine_Envelopes` (tablas `wolverine_outgoing_envelopes/incoming/dead_letters/nodes` en el schema del módulo).
- Cobertura nueva en Fase 4: atomicidad por módulo, tenant middleware, DLQ real, inbox dedupe.

**Disciplina nueva (Architecture.Test en Fase 5):** Wolverine solo en assemblies con consumidores de integration events; **no** llamar `bus.InvokeAsync` in-process (Mediator manda ahí).

## Implementación — plan por fases

1. **Wiring Wolverine + outbox EF en Identity**, en paralelo al bus propio. *(3 archivos.)*
2. **Recablear 5 publicadores** a `bus.PublishAsync(...)`; Chat/Files/Notifications/Identity enrolan DbContext + migración `Wolverine_Envelopes`. *(5 handlers + 3 módulos.)*
3. **Portar 4 consumidores** a handlers Wolverine; `WebhookFanoutHandler` → catch-all sobre `Envelope`; middleware de tenant. **Aquí Inventario queda destrabado** (hereda middleware). *(4 handlers + 1 nuevo en Webhooks.)*
4. **Mover tests** al harness de Wolverine; añadir 4 tests de cobertura nueva.
5. **Borrar bus propio (~880 LOC)** + `IEventBus`; reescribir `EventingArchitectureTests` + añadir test de disciplina Mediator/Wolverine. **Cierra ambos ADRs operativamente.**

## Riesgos y mitigación

1. 🔴 `WebhookFanoutHandler<TEvent>` open-generic — **catch-all sobre `Envelope`** (semántica idéntica, 1 archivo nuevo).
2. 🟠 Convivencia Mediator/Wolverine — disciplina `AddAssembly` selectivo + nota en `eventing.md` + Architecture.Test en Fase 5.
3. 🟠 Tablas `wolverine_*` por DbContext × N módulos — al checklist de `add-module`.
4. 🟡 Cambio de envelope RabbitMQ — drenar pre-corte en producción.
5. 🟡 Cobertura actual cero del bus — 2 tests de fuego al cierre de Fase 2.
