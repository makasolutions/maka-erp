# ADR-0004 · Bus de eventos: adoptar Wolverine v3 (MIT)

- **Estado:** Aceptado · junio 2026
- **Decisores:** Juan (CEO/sistemas) + asistente de planeación
- **Ligado a:** **ADR-0001** (outbox por módulo — resuelto en conjunto)
- **Evidencia:** `docs/specs/platform/SPEC.md` (as-built) · `docs/specs/platform/decision-ADR-0001-0004.md` (medición del bus) · `docs/specs/platform/migration-wolverine.md` (medición de migración)

---

## Contexto

El as-built reveló que el bus de eventos del repo es **propio**: `RabbitMqEventBus` sobre `RabbitMQ.Client`, un `OutboxDispatcher` como `HostedService`, un `EfCoreOutboxStore` y un `EfCoreInboxStore` también propios — **~740 LOC** entre todos los componentes de la fundación de eventing. Esto contradice lo que decían `CLAUDE.md` y `AGENTS.md`, que afirmaban "MassTransit 8.5.7 Apache 2.0" — **falso; no hay ninguna referencia a MassTransit en el código.**

La medición de acoplamiento (M1–M4 en `decision-ADR-0001-0004.md`) mostró:
- **Consumidores delgados** (mayoría LOC de negocio; cero acoplamiento a transporte crudo).
- **Bus = plomería 100% genérica** — retry con delay fijo, dead-letter por flag (no DLQ real), inbox idempotente, topic exchange, tenant en header. **Cero bespoke.**
- **RabbitMQ contenido en 1 archivo** (`RabbitMqEventBus.cs`); cero filtraciones a módulos de negocio.

Conclusión de esa medición: el bus propio es **reemplazable sin pérdida funcional**.

## Decisión

**Adoptar Wolverine v3 (MIT)** como capa de mensajería + outbox transaccional + bus, **conservando Mediator source-gen** para el flujo in-process.

Wolverine resuelve también ADR-0001 (outbox transaccional nativo por DbContext). Ambos ADRs se cierran en conjunto.

## Opciones consideradas

### Opción A — Mantener el bus propio
Conservar `RabbitMqEventBus`, dispatcher, outbox e inbox propios.

- **Pros:** cero dependencia nueva; control total.
- **Contras:** ~740 LOC propias mantenidas, testeadas y evolucionadas por nosotros para siempre, en la capa donde la corrección contable no se negocia, y hoy con **cobertura cero** de tests directos. Sin red en la pieza más delicada.

### Opción B — MassTransit (v8 / v9) — *Descartada*
- **v8 Apache 2.0** entra en end-of-life a fin de 2026. Adoptarla hoy es asumir un cimiento sin soporte en ~6 meses.
- **v9 comercial** (Massient): mínimo USD 400/mes; umbral sub-1M ambiguo y no garantizado. Dependencia comercial en la ruta crítica de un SaaS que aspira a crecer más allá de ese umbral.

La librería que `CLAUDE.md` afirmaba estar usando ya no es una opción viable hacia adelante.

### Opción C — Wolverine v3 (MIT) — *Elegida*
Mensajería + outbox transaccional + bus, con handlers descubiertos por convención. Construido por Jeremy Miller (Marten) — autor con dominio profundo del patrón outbox sobre EF Core / Postgres.

- **Outbox transaccional nativo** por DbContext (resuelve ADR-0001 sin código propio).
- **Inbox idempotente nativo**, **DLQ real** (cola/exchange dead-letter), **retry con backoff exponencial** estándar.
- **Harness de tests determinista** (`TrackedSessions` + in-memory transport).
- **MIT** — sin EOL, sin costo, forkeable si fuera necesario.
- **Convive con Mediator** — son universos disjuntos: Mediator atiende comandos in-process, Wolverine atiende integration events. La disciplina ("no `bus.InvokeAsync` in-process; `AddAssembly` selectivo") se codifica como Architecture.Test en Fase 5.

### Opciones D — NServiceBus / Rebus / Brighter
- **NServiceBus**: comercial, descartada por la misma razón que MassTransit v9.
- **Rebus** (MIT): outbox manual, sin convenciones; menos cobertura del patrón que Wolverine.
- **Brighter** (MIT): viable, pero menor tracción y diferenciador que Wolverine en .NET 10 + EF Core 10.

## Validación upstream (FSH / Mukesh)

- Mukesh evaluó Wolverine en **issue #894** (oct-2023) y **discusión #870** (.NET 8 migration); decidió **quedarse con Mediator source-gen** y no adoptar Wolverine.
- Su razonamiento: *"OSS estándar, plain DI, sin frameworks de los que no puedas salir."*
- **Divergimos conscientemente** — pero solo en la capa de mensajería/outbox. **Mediator source-gen se conserva** para el flujo in-process, donde Mukesh tiene razón y donde no necesitamos lo que Wolverine ofrece. Wolverine entra exclusivamente como bus + outbox transaccional + inbox, no como mediator alternativo.

## Consecuencias

**Estructurales:**
- INV-8 reformulado sobre `IMessageBus` con regla "publicación enrolada en DbContext" (Architecture.Test reescrito en Fase 5).
- INV-9 (tenant) estructural por middleware.

**Operativas:**
- Paquetes nuevos: `WolverineFx`, `WolverineFx.RabbitMQ`, `WolverineFx.EntityFrameworkCore` (todos MIT, sobre `net10.0` + EF Core ≥10.0.2 + RabbitMQ.Client ≥7.1.2 — compatibilidad verificada).
- Se borran ~880 LOC propias (14 archivos de `Eventing/*` + `IEventBus.cs`); se conserva `IIntegrationEvent.cs`.
- Cobertura nueva: atomicidad por módulo, tenant middleware, DLQ real, inbox dedupe.

**De documentación:**
- `CLAUDE.md` y `AGENTS.md` se corrigen (de "MassTransit 8.5.7" a "Wolverine v3 MIT").
- `eventing.md` se reescribe sobre el nuevo modelo, con la regla de convivencia Mediator/Wolverine.

## Implementación
Ver plan de fases en ADR-0001. Inventario queda destrabado al cierre de Fase 3.

## Riesgos
1. 🔴 `WebhookFanoutHandler<TEvent>` open-generic — catch-all sobre `Envelope` (mitigación lista).
2. 🟠 Disciplina Mediator/Wolverine — Architecture.Test en Fase 5.
3. 🟡 Cambio de envelope en producción — drenar pre-corte.
