# Nota de Correcciones de Doctrina — 2026-06-19

> **Propósito:** Registrar explícitamente qué doctrina de la documentación anterior (en Drive viejo y en
> NotebookLM) quedó **obsoleta**, para guiar la limpieza de fuentes. Las fuentes que afirmen lo de la
> columna "obsoleto" deben corregirse o eliminarse del notebook y reemplazarse por los documentos nuevos
> del repo (`docs/architecture/`).
> **Esta nota NO es fuente de verdad permanente** — es un instrumento de transición. Una vez limpiado el
> notebook y alineada la doctrina, su utilidad decae. La fuente de verdad permanente es el repo `docs/`.

---

## Por qué existe esta nota

Durante meses, la documentación del proyecto se acumuló en Drive y se subió a NotebookLM. Parte de esa
documentación refleja decisiones que **ya cambiaron** — y otra parte nunca fue cierta (describía un stack
planificado que no se construyó así). NotebookLM no permite editar fuentes (solo agregar), así que mezcla
doctrina vieja con nueva al responder — como se observó al citar "MassTransit Sagas" cuando MassTransit
nunca estuvo en el código. Esta nota lista las correcciones para que la limpieza sea quirúrgica.

---

## Correcciones de doctrina (verificadas contra el repo, 2026-06-19)

### 1. Bus de mensajería: MassTransit → Wolverine
- **Obsoleto:** "MassTransit 8.5.7", "MassTransit Sagas para flujos con estado".
- **Vigente:** **Wolverine** es el bus de integration events cross-módulo. **MassTransit NUNCA estuvo en
  el código** — el grep en `src/**` devuelve 0 (verificado en el as-built de Plataforma y en ADR-0004).
  No es que se "erradicó": nunca existió; era doctrina falsa heredada de un plan inicial. Para flujos con
  estado: Wolverine. Para jobs recurrentes: Hangfire.
- **⚠️ Residuo en el repo:** `AGENTS.md` **todavía** lista "MassTransit 8.5.7" en la tabla de stack y en la
  golden rule #13. Eso es residuo a corregir, no la verdad operativa. La fuente vigente del eventing es
  `.agents/rules/eventing.md`.
- **Dónde más aparece el error:** módulo de Automatizaciones (RF-AUTO-4), sincronización WooCommerce (RF-CAT-7).

### 2. Mediador CQRS: MediatR → Mediator source-generated
- **Obsoleto:** "MediatR".
- **Vigente:** el **Mediator source-generated** del boilerplate FSH (no MediatR). Wolverine NO reemplaza
  al Mediator interno; conviven en universos disjuntos (Wolverine = integration events cross-módulo;
  Mediator = comandos/queries in-process intra-módulo).

### 3. Bus de eventos propio (legacy) → entrega local Wolverine
- **Obsoleto:** describir el bus de eventos propio (`RabbitMqEventBus` + `IEventBus` + outbox/inbox custom,
  ~880 LOC) como la vía de integración definitiva.
- **Vigente:** ese bus propio está **en retiro vía la migración Wolverine de 5 fases** (Fase 3 cerrada;
  **Fase 5 borra el bus propio**). El bus legacy **operaba** (no es cierto que "nunca entregó"); simplemente
  se reemplaza por Wolverine con **entrega local in-process** + durabilidad Postgres. RabbitMQ queda
  reservado para salida real a otros procesos (microservicios futuros), no para eventing interno.
- *(Corrección de una nota previa de esta misma sesión que afirmaba que el bus legacy "nunca entregó
  end-to-end": era un overclaim — confundía el bus legacy con el bug de la 5ª capa de la migración Wolverine.)*

### 4. Transporte de eventos: RabbitMQ obligatorio → entrega local
- **Obsoleto:** "RabbitMQ" como transporte de eventos internos; el harness/dev requería broker arriba.
- **Vigente:** **entrega local in-process** para el monolito (local durable queues de Wolverine, respaldo
  Postgres, tablas `wolverine_*`), sin broker. NO usar `PublishMessage<T>().ToRabbitExchange(...)` salvo
  consumidor externo real. Dev y tests NO necesitan RabbitMQ.

### 5. Real-time: SignalR y SSE CONVIVEN (uno no reemplaza al otro)
- **Obsoleto/impreciso:** afirmar que "SignalR reemplaza a SSE" o viceversa.
- **Vigente:** **ambos coexisten, con roles distintos.** **SignalR** (`AppHub` en `/api/v1/realtime/hub`)
  para bidireccional/colaborativo (chat, presencia, push de notificaciones — p. ej. el flujo [REAL]
  `MentionedInChannel`). **SSE** (handshake de token en dos pasos) para unidireccional servidor→cliente.
  Regla: flujo unidireccional → SSE; bidireccional/colaborativo → SignalR. Fuente: `.agents/rules/realtime.md`.

### 6. Frontend: Blazor → React 19 (dos apps)
- **Obsoleto:** "Blazor / MudBlazor", "purgar MudBlazor y montar Syncfusion **Blazor**".
- **Vigente:** **React 19 + Vite 7 + TypeScript** (NO Blazor). **DOS apps:** `clients/admin` (operador, :5173)
  y `clients/dashboard` (tenant, :5174). TanStack Query v5, React Router 7, Radix + Tailwind v4 + CVA.
  **Syncfusion 33.x SOLO vía wrappers Maka*** (`MakaGrid`/`MakaChart`/`MakaKanban`/`MakaPivot`/`MakaScheduler`).
  i18next ES/EN (`t()`, ES primero).
- **⚠️ Residuo en el repo:** `CLAUDE.md §6` (Fase 0) todavía describe la purga de MudBlazor + setup
  Syncfusion **Blazor**. Es obsoleto — residuo del plan inicial. A corregir (ver lista quirúrgica aparte).

### 7. Persistencia: (Marten evaluado) → EF Core confirmado
- **Obsoleto:** cualquier sugerencia de migrar a Marten / Critter Stack completo.
- **Vigente:** **EF Core 10** sobre PostgreSQL 17. Marten evaluado a fondo (2026-06-19) y **descartado**
  para un ERP relacional: la propia doc de Marten lo desaconseja para dominios con relaciones complejas;
  el Core de FSH (multitenancy Finbuckle, soft-delete, audit, specifications) está atado a EF Core.

### 8. Multitenancy: shared-database (default-ON)
- **Vigente:** **Finbuckle, shared-database** (un solo DB físico, discriminador `TenantId` + global query
  filters de EF, aislamiento **default-ON** vía `BaseDbContext`; opt-out solo vía `IGlobalEntity`).
  Implicación: la multitenancy nativa de Wolverine (que es database-per-tenant) NO aplica; el tenant en
  consumidores se restaura estructuralmente vía `TenantContextMiddleware` (Fase 3) y, para escritura,
  con el helper `TenantScopedDbContext`.

### 9. Costeo de inventario: UEPS eliminado
- **Obsoleto:** UEPS como método de costeo.
- **Vigente:** solo **PEPS, promedio ponderado e identificación específica** (UEPS prohibido por NIIF).
  *(Esta corrección ya estaba en la doc; se confirma.)*

---

## Distinción transversal: roadmap vs repo

Más allá de las correcciones puntuales, la documentación vieja tiene un problema sistémico: **mezcla lo que
debería existir (diseño/roadmap) con lo que existe (código)**. Ejemplos verificados:

- Eventos como `PartyCreated`, `OrderPaid`, `SupplierProfileActivated` se describen como parte del flujo
  real, pero **no existen en el código** — son diseño futuro. Solo 4 integration events son [REAL] hoy
  (ver `system-module-map.md` §5).
- El SPEC de ContactList decía campos de persona "obligatorios"; decisión actualizada: **opcionales**
  (alineado con todos los referentes: Siigo, Effi, Folk, Odoo — ninguno los hace obligatorios). Nota: el
  `parties/SPEC.md` as-built del repo ya refleja esto (migración v1→v2 completa).

**Regla para la limpieza:** al revisar cada fuente del notebook, marcar si describe diseño o estado real.
Las fuentes de diseño puro (roadmap, SPECs aspiracionales) pueden quedar como *referencia de diseño*, pero
deben señalarse como tales. Las fuentes que afirman doctrina técnica obsoleta (tablas de §1-9) deben
corregirse o eliminarse.

---

## Plan de limpieza del notebook (acción de Juan)

1. Agregar al notebook los 3 documentos nuevos del repo (`system-module-map.md`, `module-deep-dive.md`,
   esta nota) como fuentes autoritativas.
2. Identificar y **eliminar** del notebook las fuentes que afirman la doctrina obsoleta de §1-9
   (especialmente las que mencionan MassTransit, MediatR, el bus legacy, RabbitMQ como transporte interno,
   o Blazor/MudBlazor como frontend).
3. Las fuentes de referentes externos (Siigo, Attio, Folk, HubSpot, Odoo, etc.) se **conservan** — son la
   biblioteca de investigación, no doctrina del sistema.
4. Resultado: notebook coherente, sin contradicciones, alineado con el repo. El repo `docs/` manda; el
   notebook lo refleja como capa de consulta.

> ⚠️ Recordatorio: corregir el notebook **no** corrige el repo. Los residuos en `AGENTS.md` (MassTransit) y
> `CLAUDE.md §6` (Blazor) viven en el repo y se corrigen ahí, vía Claude Code, con la lista quirúrgica aparte.

---

## Fuentes nuevas de referentes a agregar (investigación pendiente)

Para enriquecer la biblioteca de investigación (no doctrina), agregar al notebook:
- **Folk** (modelo de datos, diccionario de campos): enlaces oficiales `help.folk.app/en/articles/9790806-folk-data-model`,
  `help.folk.app/en/articles/4991006-contact-fields`, `developer.folk.app/core-concepts/custom-fields`
  (identificados el 2026-06-19). Folk aporta el patrón de campos nativos vs custom scopeados por workflow —
  relevante para el diseño parametrizable del CRM.
