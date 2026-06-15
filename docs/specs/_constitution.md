# Índice maestro / Constitución — Maka ERP

> **Propósito:** tejido conectivo entre los documentos definitivos y constitución del sistema. Cada documento es dueño de **una capa**; este índice dice **quién define qué**, la fuente de verdad por tema, y las decisiones que cruzan todos los módulos. No duplica contenido: conecta.
> **Última actualización:** junio 2026 · UEPS eliminado · as-built de **Catalog** y **Plataforma** registrados · **ADR-0001 + ADR-0004 ACEPTADOS** (Wolverine v3 MIT — outbox transaccional por módulo, conserva Mediator) · plan de migración por 5 fases · regla de validación upstream (FSH/Mukesh) anclada.

---

## 1. Las tres capas / documentos

| # | Documento | Capa | Fuente de verdad para |
|---|---|---|---|
| **A** | `especificacion_sistema_integral_unificado` (Drive) | **Visión funcional** (14 módulos) | Alcance, qué módulos existen, flujos transversales, exclusiones, roadmap. *Contexto — partes superadas por modelado posterior (ver §7).* |
| **B** | `Modelo-Modulos-Campos-MakaERP-v2` (Drive) | **Modelo relacional / campos** | Tercero+roles, datos fiscales DIAN, direcciones/VOs, CRM, omnicanal, catálogo/producto, orden-en-chat, usuarios/empresa/permisos, campos personalizados. |
| **C** | `SPEC-Inventario-MakaERP` (forward, en curso) | **Inventario / Disponibilidad** | Fuente/Disponibilidad, inventario propio (kardex, valoración, lotes/seriales, conteos/ajustes/auditoría, ubicaciones/WMS), cumplimiento por línea, traslados/tránsito, subledger↔GL. |

A medida que se documenta, cada módulo gana su propio SPEC (as-built o forward). Mapa en §2 (visión) y §3 (código existente).

---

## 2. Mapa de los 14 módulos (visión) → dónde vive cada uno

| Módulo (visión A) | Documento | Estado |
|---|---|---|
| M1 · CRM y Leads | **B** + A | Modelado relacional |
| M2 · E-commerce y OMS | A + **B** | Esbozado |
| M3 · **Inventario y Almacén** | **C** (`SPEC-Inventario`) | **SPEC forward iniciado** (CAP-01/02 escritas) — destrabado al cierre de Fase 3 del plan ADR-0001/0004 |
| M4 · Compras e **Importaciones** | A | Pendiente — patrón "en tránsito" con C |
| M5 · Ventas y Pipeline | **B** + A | Modelado relacional |
| M6 · Mensajería IA | **B** + A | Modelado relacional |
| M7 · **Contabilidad y Finanzas** | A + **C** (subledger↔GL) | Costura definida; módulo pendiente |
| M8 · Facturación electrónica DIAN | A | Pendiente |
| M9 · Logística y Despacho | A | Pendiente |
| M10 · **Garantías y Postventa** | A + **C** (serial→garantía) | Puente definido |
| M11 · **Dropshipping y Proveedores** | A + **C** (Fuente externa) | Puente definido |
| M12 · BI | A | Pendiente |
| M13 · Workflows | A | Pendiente |
| M14 · Integraciones y API | A | Pendiente |

---

## 3. Módulos existentes en código (as-built)

| Módulo (código) | Spec as-built | Estado |
|---|---|---|
| **Catalog** | `docs/specs/catalog/SPEC.md` | ✅ As-built completo (ver §5) |
| **Fundación de Plataforma** (Multitenancy + Outbox/Eventing + BuildingBlocks) | `docs/specs/platform/SPEC.md` | ✅ As-built completo — **revela la compuerta ADR-0001** (ver §5/§6) |
| Identity | — | Pendiente |
| Chat | — | Pendiente |
| Files | — | Pendiente |
| Hr (parcial, ~10%) | — | Pendiente |

---

## 4. Decisiones transversales (válidas en TODOS los documentos)

- **UEPS ELIMINADO.** Solo **PEPS, promedio ponderado e identificación específica** (NIIF/NIC 2/Decreto 2420).
- **Tercero (Party) + roles** como agregado raíz; `OwnerId Guid?` para multitenant.
- **Eventos como tejido de integración.** Outbox **transaccional por módulo (Wolverine v3)**; los módulos **consumen eventos**, no comparten tablas. El **kardex es el subledger**; el GL solo tiene cuentas de control. *(Migración en curso desde el bus propio — ver §6.)*
- **Serial → identificación específica + garantía.**
- **Patrón "en tránsito" compartido** (traslados inter-bodega ≡ importaciones).
- **Doctrina "nada se sobrescribe, todo se versiona"** → auditoría gratis.
- **Parametrizable y compliance-nativo.** Reglas DIAN como configuración; sistema **permanente**; menor entre costo y VNR; costeo por categoría con override por serial.
- **Cada etapa vale por sí sola.** Single-tenant útil desde el día 1.
- **Extracción de bounded contexts en Catalog (ADR-0003).** Extraer `SupplierRelations/Convenios` (8 tipos) junto con su rediseño Fase B — no antes; `Marketplace` (5 tipos) permanece "hasta que duela". Nada se extrae aún.
- **Regla de validación upstream (FSH / Mukesh).** Toda decisión de arquitectura importante (ADRs, elección de librerías/dependencias estructurales, cambios en BuildingBlocks/patrones transversales, decisiones de bounded context) se **valida contra `codewithmukesh.com` y `github.com/fullstackhero/dotnet-starter-kit`** antes de cerrarse. El propósito es triple: (1) ver si el upstream ya resolvió o desestimó el problema, (2) detectar cuándo divergimos y dejar la divergencia consciente y documentada, (3) usar el path de Mukesh como **datapoint, no como autoridad** — si divergimos, lo hacemos a propósito con su razonamiento conocido. Cada ADR debe incluir una sección "Validación upstream: …" con el hallazgo. **No aplica** a decisiones de dominio de negocio (valoración de inventario, reglas DIAN, modelo Fuente/Disponibilidad) ni a decisiones internas de un módulo de negocio — el starter es agnóstico al dominio.

---

## 5. Deuda conocida (as-built)

- ✅ **Outbox atómico solo para Identity** — **resuelto en plan** (ADR-0001/0004 aceptados; Wolverine outbox por módulo en migración por fases). Inventario destrabado al cierre de Fase 3.
- ✅ **MassTransit no existe en código** — **decisión tomada:** se adopta **Wolverine v3 (MIT)** (no MassTransit). `CLAUDE.md`/`AGENTS.md` se corrigen como parte de la migración.
- ✅ **Tenant-restore manual** — **se vuelve estructural** con el middleware de Wolverine (Fase 3).
- ✅ **Cobertura de fundación casi nula** — se paga en Fase 4 (atomicidad por módulo, tenant middleware, DLQ real, inbox dedupe).
- **Fuga de stock en Catalog** (`ManageStock` + permiso huérfano `AdjustStock`). → se limpia cuando Inventario tome ownership del stock.
- **Timestamps manuales.** Catalog (24 entidades) y aguas abajo usan `CreatedAtUtc` manual en vez de `IAuditableEntity` (baseline M6). → batch transversal, bajo riesgo.
- **Cobertura tests catálogo-núcleo.** 126 endpoints, 0 integration tests. → backlog del núcleo (no CAP-04/05, que van a rediseño).
- **No-violaciones esperadas:** `Agreement` sin versionar (placeholder, llega con Convenios Fase B); INV-8 "vacío" en Catalog (será productor cuando Inventario/Pricing reaccionen a cambios de catálogo).

---

## 6. Registro de ADRs

| ADR | Tema | Estado |
|---|---|---|
| **ADR-0001** | **Outbox transaccional por módulo** (resuelto vía Wolverine: envelope persistido en el `SaveChanges` del DbContext del módulo → atomicidad estructural; middleware de tenant → INV-9 estructural). **Validación upstream:** eventing actual es divergencia local; no hay solución upstream. Ver `docs/adr/ADR-0001-outbox-por-modulo.md`. | ✅ **Validado estructuralmente (Fase 2)** — primer publicador real (`UserRegisteredIntegrationEvent`) emite por Wolverine vía outbox transaccional + entrega RabbitMQ verificada E2E (`WolverineUserRegisteredE2ETests`). 5ª capa cerrada con causa raíz documentada: **bug de uso del API** (`PublishAsync` solo encola; falta `SaveChangesAndFlushMessagesAsync` para persistir el envelope). Hipótesis original de "colisión de interceptors" invalidada con evidencia: Wolverine no usa `ISaveChangesInterceptor`, usa codegen frames en el pipeline del handler. Ver `docs/specs/platform/wolverine-phase1-followups.md`. Fase 3 destraba Inventario. |
| **ADR-0002** | Fundación de eventos (`OccurredOnUtc` → `DateTimeOffset`, opción A) | Aceptada |
| **ADR-0003** | Extracción de bounded contexts de Catalog (`SupplierRelations/Convenios` → Fase B; `Marketplace` permanece). **Validación upstream:** Catalog del starter es vertical-slice mínimo; nuestro Catalog es divergencia consciente por necesidad de negocio. | Propuesta |
| **ADR-0004** | **Bus de eventos: Wolverine v3 (MIT)** (MassTransit descartado: v8 EOL fin 2026; v9 comercial). Conserva Mediator source-gen para in-process. **Validación upstream:** Mukesh decidió no adoptar Wolverine (issue #894 / discusión #870); divergimos solo en mensajería/outbox, conservando Mediator para in-process. Ver `docs/adr/ADR-0004-bus-wolverine.md`. | ✅ **Aceptado** — implementación en conjunto con ADR-0001 |

---

## 7. Discrepancias visión (A) ↔ modelado (C) — qué manda

| Tema | En A (mayo) | Qué manda hoy |
|---|---|---|
| Valoración de inventario | "PEPS, **UEPS**, Promedio" | **C** — UEPS fuera; tres métodos legales |
| Profundidad de Inventario | Plano (stock por SKU/bodega) | **C** — Fuente/Disponibilidad, cumplimiento por línea, kardex inmutable |
| **Marketplace multi-vendor** | **Excluido** de Fase 1 | **Evolución estratégica:** corazón del modelo (C + Convenios Fase B). *Prioridad pendiente de confirmar.* |

---

## 8. Estructura del corpus de specs

- **Nivel sistema — Constitución** (este documento): principios, decisiones transversales, mapas, ADRs, deuda. Single source of truth. **No** es un mega-spec.
- **Nivel módulo — un SPEC por módulo** (estilo `SPEC-Inventario`): requirements EARS → design → tasks → harness. Unidad construible.
- **Nivel capacidad** (CAP-NN): si un módulo crece, una capacidad se parte en su archivo.

```
docs/specs/
  _constitution.md
  catalog/SPEC.md          ← as-built (hecho)
  platform/SPEC.md         ← as-built (hecho)
  platform/decision-ADR-0001-0004.md       ← medición del bus
  platform/migration-wolverine.md          ← medición de migración
  inventory/SPEC.md        ← forward (en curso, destrabado al cierre de Fase 3)
  ...
docs/adr/
  ADR-0001-outbox-por-modulo.md
  ADR-0002-occurredon-utc-datetimeoffset.md
  ADR-0004-bus-wolverine.md
  ...
```

**Orden:** as-built de lo existente **antes** de los forward que dependan de ello. **Cerrar cada tema antes de abrir el siguiente.** Próximo bloque: ejecutar el plan de migración Wolverine por fases (3 destraba Inventario, 5 cierra ambos ADRs operativamente).

---

## 9. Pendientes de modelar (cola)

- **Migración Wolverine (ADR-0001/0004 — aceptados):** Fase 1 wiring · Fase 2 publicadores · **Fase 3 consumidores (destraba Inventario)** · Fase 4 tests · Fase 5 borrar bus propio. Más fix de `CLAUDE.md`/`AGENTS.md` (MassTransit → Wolverine) en el wiring.
- **As-built restante:** Identity, Chat, Files, Hr.
- **Forward:** Inventario CAP-03..09 · Importaciones · Garantías · Compras · Facturación DIAN · Logística · Contabilidad · BI · Workflows · Integraciones.
- **Fase B:** Convenios-marketplace (gatilla ADR-0003).

---

## 10. Documentos de contexto (no editar — solo referencia)

En Drive: `effi_erp_documentacion_*`, `Resumen Reqs`, `CONTEXTO MAKA-ERP`. De repo: `CLAUDE.md` y `GUIA_INICIO_CLAUDE_CODE` *(⚠️ corregir la mención a MassTransit)*.
