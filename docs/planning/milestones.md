# Maka ERP — Plan de Milestones

> **Propósito:** mapa ejecutivo del proyecto. Define QUÉ se construye en cada etapa, PARA QUIÉN, y el criterio de salida que confirma que el milestone está cerrado. Este documento orienta el orden de los SPECs a escribir y el orden de implementación.
> **Última actualización:** junio 2026
> **Fuente de verdad para:** priorización de módulos, orden de documentación, criterios de lanzamiento.
> **No duplica:** la constitución (_constitution.md) define decisiones técnicas; el tech-roadmap define tecnologías; este documento define valor de negocio y secuencia.

---

## Resumen ejecutivo

```
M0 — Core técnico     M1 — MVP interno      M2 — Reemplaza Effi    M3 — SaaS multi-cliente
~80% completo         ~3-4 meses desde M0   ~4-6 meses desde M1    ~6-9 meses desde M2

Wolverine, eventos,   Parties, Catálogo,    Facturación DIAN,       Onboarding self-service,
NamingSeries,         Cotizaciones,         Contabilidad,           Billing SaaS,
ISubmittable,         Órdenes de Venta,     Importaciones,          Marketplace multi-vendor,
Fase 5 (bus)          Inventario básico,    Logística,              Integraciones externas,
                      Compras, CRM básico,  Garantías y Postventa,  pgvector + búsqueda IA,
                      Dashboard BI básico   Mensajería IA +         Workflows configurables,
                                            WhatsApp omnicanal,     BI avanzado
                                            WMS, Nómina
```

**Usuario objetivo por milestone:**
- M0: solo equipo técnico
- M1: Juan + equipo comercial TecnoImportaciones
- M2: toda la empresa TecnoImportaciones (reemplaza Effi)
- M3: clientes externos de Maka Solutions SaaS

---

## Milestone 0 — Core técnico listo

**Objetivo:** la plataforma puede correr en producción de forma confiable con las abstracciones de dominio correctas. Sin esto, nada de lo que se construya encima es seguro.

**Estado actual:** ~80% completo.

### Qué incluye

| Componente | Estado |
|---|---|
| Pipeline de eventos Wolverine (Fases 1-4) | ✅ Completo |
| Multitenancy Finbuckle | ✅ Completo |
| Auth JWT + Identity | ✅ Completo |
| OpenTelemetry + observabilidad | ✅ Completo |
| `ISubmittable` + `DocStatus` (ADR-0006) | ✅ Completo |
| NamingSeries dominio + contratos (PR1) | ✅ Completo |
| NamingSeries migración + CRUD + allocator (PR2) | ⏳ Pendiente |
| Fase 5: borrar bus propio (~932 LOC) | ⏳ Pendiente |
| Architecture.Tests 51/51 | ✅ Completo |

### Criterio de salida

Sistema corre en staging, multi-tenant, con eventos transaccionales, numeración DIAN funcional, y sin deuda técnica del bus propio.

---

## Milestone 1 — MVP Interno TecnoImportaciones

**Objetivo:** Juan y el equipo de TecnoImportaciones pueden operar el flujo comercial principal sin salir de Maka ERP. Corre en paralelo a Effi como validación real.

**Usuario:** equipo comercial TecnoImportaciones (Juan, Sandra, Dir. Creativo).

### Módulos M1

| Módulo | Descripción | Prioridad dentro de M1 |
|---|---|---|
| **Parties / Terceros** | Clientes (1.897), Proveedores (204), Empleados (24), Contactos. Patrón Tercero base + roles. Datos fiscales DIAN (NIT, régimen, CIIU). | 1 — base de todo lo demás |
| **Catálogo extendido** | Ya existe parcial. Completar: precios múltiples, atributos variables (color, talla, serial), imágenes, categorías, marcas (Sony, DJI, Canon, Nanlite, DZOFilm, etc.). | 2 — base de OV y cotizaciones |
| **Cotizaciones** | Draft → Enviada (Submitted) → Aceptada/Rechazada. Versionamiento (AmendedFrom). Conversión a OV. Aplica ISubmittable. Tarifas múltiples por cliente. | 3 — flujo comercial principal B2B |
| **Órdenes de Venta** | Creada desde cotización aceptada o directa. Reserva de stock. Estados: Draft → Confirmada → En despacho → Entregada. | 4 |
| **Inventario básico (CAP-03..05)** | Entradas (compras, ajustes positivos), Salidas (ventas, ajustes negativos), Ajustes. Trazabilidad serial/lote. 7 bodegas. Costeo promedio. | 5 |
| **Compras** | Orden de Compra + Remisión de entrada (GRN). ISubmittable en OC (aprobada = inmutable). CxP básico. | 6 |
| **NamingSeries operativo** | Numeración de Cotizaciones, OV, OC. UI de gestión de series. Alertas 80%. | 7 |
| **CRM básico** | Oportunidades + Seguimientos comerciales. Vinculados a Tercero. Pipeline Kanban básico. | 8 |
| **Dashboard BI básico** | Ventas del día/semana/mes. Stock crítico. CxC/CxP resumen. Top productos. | 9 |

### Lo que NO entra en M1

- Facturación electrónica DIAN (requiere certificado digital + proceso de homologación)
- Contabilidad formal
- Nómina
- Logística (transportadoras, guías)
- Mensajería IA / WhatsApp
- WMS (ubicaciones en bodega)
- Dropshipping / Convenios

### Flujo principal de M1

```
Tercero (Cliente) → Cotización (Draft → Enviada → Aceptada)
                         ↓ si acepta
                    Orden de Venta → Reserva de stock en Inventario
                         ↓
                    [Futuro M2] Remisión + Factura DIAN

Proveedor → Orden de Compra (Draft → Aprobada)
                ↓ al recibir
            Remisión de entrada → Incrementa stock en Inventario
```

### Criterio de salida de M1

Juan puede:
1. Crear un cliente nuevo con datos fiscales DIAN.
2. Crear una cotización con múltiples productos y tarifas, enviarla, y convertirla en OV.
3. Ver el stock disponible en tiempo real y confirmar una OV.
4. Crear una OC a un proveedor y registrar la entrada de mercancía.
5. Ver el dashboard con ventas del día y stock crítico.

Todo esto sin abrir Effi.

### SPECs a escribir para M1 (en orden)

1. `docs/specs/parties/SPEC.md` — Terceros + roles + datos DIAN
2. `docs/specs/catalog/SPEC-extended.md` — Catálogo extendido (sobre el as-built existente)
3. `docs/specs/sales/SPEC-quotations.md` — Cotizaciones
4. `docs/specs/sales/SPEC-orders.md` — Órdenes de Venta
5. `docs/specs/inventory/SPEC.md` CAP-03..05 — Entradas, Salidas, Ajustes (sobre CAP-01/02 existentes)
6. `docs/specs/purchasing/SPEC.md` — Compras básicas

---

## Milestone 2 — Operación completa TecnoImportaciones

**Objetivo:** Maka ERP reemplaza completamente a Effi. TecnoImportaciones puede cancelar su suscripción de Effi ($$$).

**Usuario:** toda la empresa TecnoImportaciones (incluyendo Yuly CFO, Daniela Contabilidad, Sandra Gerente Medellín).

### Módulos M2

| Módulo | Descripción | Sub-prioridad |
|---|---|---|
| **Facturación electrónica DIAN (M8)** | Facturas, Notas Crédito/Débito. ISubmittable. NamingSeries con resolución DIAN. Firma digital. Envío a DIAN. | Alta — requisito legal |
| **Contabilidad (M7)** | Plan de cuentas NIIF, Comprobantes contables, Cuentas por Cobrar/Pagar detalladas, Conciliación bancaria. Subledger ↔ GL por eventos. | Alta |
| **Importaciones (M4)** | Liquidación de costos (landed cost), DIN, gestión de tránsito internacional, derechos de aduana. Patrón "en tránsito" compartido con Inventario. | Alta — core del negocio TecnoImportaciones |
| **Logística y Despacho (M9)** | Guías, transportadoras (Servientrega, Coordinadora, etc.), relaciones de despacho, novedades, recolecciones. | Media |
| **Garantías y Postventa (M10)** | Trazabilidad serial → venta → garantía. RMA. Tiempo de respuesta. Proveedores de garantía. | Media |
| **Mensajería IA + WhatsApp omnicanal (M6)** | WhatsApp Business API, chat omnicanal (WhatsApp + web + SMS), catálogo en chat, CRM integrado, chatbot básico, campañas masivas. IA asistida al agente (sugerencias, resumen historial). | Media — reemplaza EffChat + Mercately |
| **Dropshipping / Convenios (M11)** | 45.489 artículos de proveedores. Convenios de distribución. Catálogo compartido. | Media |
| **WMS — Ubicaciones en Bodega** | 4 niveles de ubicación (Effi). Ingresos, Egresos, Movimientos por ubicación. | Baja-Media |
| **Nómina (M7 complemento)** | 24 empleados. Nómina electrónica DIAN. Ausencias, Anticipos, Conceptos de nómina. Integración con Contabilidad. | Baja — al final de M2 |

### Notas de diseño M2

**Mensajería IA no es "básica":** requiere catálogo estable (M1), CRM funcionando (M1), y WhatsApp Business API homologada. La IA real (RAG sobre catálogo con pgvector, similarity de clientes) va al final de M2 o principio de M3.

**WMS y Nómina al final de M2:** se construyen después de que el flujo comercial principal (facturación, compras, logística) ya esté funcionando.

**Facturación DIAN prerequisitos:** NamingSeries operativo (M0/M1) + ISubmittable (M0) + certificado digital (gestión externa Juan).

### Criterio de salida de M2

Maka ERP puede procesar el ciclo completo: Lead → Cotización → OV → Compra → Importación → Recepción → Despacho → Factura DIAN → Pago → Contabilidad. TecnoImportaciones cancela Effi.

---

## Milestone 3 — Plataforma SaaS Multi-cliente

**Objetivo:** Maka ERP puede venderse a otros clientes colombianos como plataforma SaaS. TecnoImportaciones es el cliente 0. Hay al menos 2-3 clientes adicionales en producción.

**Usuario:** clientes externos de Maka Solutions SaaS.

### Componentes M3

| Componente | Descripción |
|---|---|
| **Onboarding self-service** | Creación de tenant sin intervención técnica. Wizard de configuración inicial. |
| **Billing SaaS** | Planes de suscripción, cobro automático, facturación SaaS. |
| **Marketplace multi-vendor (Fase B)** | Convenios cross-tenant, catálogo compartido entre proveedores y distribuidores. ADR-0003. |
| **Integraciones externas (M14)** | WooCommerce, Mercado Libre, Falabella, B&H Photo. API pública. |
| **BI avanzado (M12)** | Reportes por industria, benchmarks cross-tenant (anonimizados), proyecciones. |
| **Workflows configurables (M13)** | Aprobaciones configurables, automatizaciones, reglas de negocio declarativas. |
| **Polly + resiliencia** | Estabilidad para múltiples clientes concurrentes con integraciones externas que fallan. |
| **pgvector + búsqueda semántica** | Búsqueda de catálogo IA, CRM similarity, RAG para Mensajería IA. |
| **Marten (event store)** | Para módulos con kardex append-only (Inventario, Contabilidad) si escala lo justifica. |

### Criterio de salida de M3

3 clientes externos pagando en producción, con onboarding autónomo (sin intervención de Juan).

---

## Orden de SPECs a escribir

Derivado de los milestones. La documentación precede a la implementación.

### Inmediatos (para M1)

| # | SPEC | Módulo | Depende de |
|---|---|---|---|
| 1 | `docs/specs/parties/SPEC.md` | Parties / Terceros | Nada — es la base |
| 2 | `docs/specs/catalog/SPEC-extended.md` | Catálogo extendido | Parties |
| 3 | `docs/specs/sales/SPEC-quotations.md` | Cotizaciones | Parties + Catálogo + ISubmittable |
| 4 | `docs/specs/sales/SPEC-orders.md` | Órdenes de Venta | Cotizaciones + Inventario |
| 5 | `docs/specs/inventory/SPEC.md` CAP-03..05 | Inventario operativo | Catálogo |
| 6 | `docs/specs/purchasing/SPEC.md` | Compras básicas | Parties + Inventario + ISubmittable |

### Para M2 (después de M1)

| # | SPEC | Módulo |
|---|---|---|
| 7 | `docs/specs/billing/SPEC-dian.md` | Facturación DIAN |
| 8 | `docs/specs/accounting/SPEC.md` | Contabilidad |
| 9 | `docs/specs/imports/SPEC.md` | Importaciones |
| 10 | `docs/specs/logistics/SPEC.md` | Logística |
| 11 | `docs/specs/warranties/SPEC.md` | Garantías |
| 12 | `docs/specs/messaging/SPEC.md` | Mensajería IA + WhatsApp |
| 13 | `docs/specs/dropshipping/SPEC.md` | Dropshipping / Convenios |
| 14 | `docs/specs/wms/SPEC.md` | WMS |
| 15 | `docs/specs/hr/SPEC-payroll.md` | Nómina |

### Para M3 (después de M2)

| # | SPEC | Módulo |
|---|---|---|
| 16 | `docs/specs/saas/SPEC-onboarding.md` | Onboarding self-service |
| 17 | `docs/specs/saas/SPEC-billing.md` | Billing SaaS |
| 18 | `docs/specs/marketplace/SPEC.md` | Marketplace multi-vendor |
| 19 | `docs/specs/integrations/SPEC.md` | Integraciones externas |
| 20 | `docs/specs/bi/SPEC.md` | BI avanzado |
| 21 | `docs/specs/workflows/SPEC.md` | Workflows configurables |

---

## Relación con documentos existentes

| Documento | Rol respecto a milestones |
|---|---|
| `_constitution.md` | Decisiones técnicas transversales que aplican a todos los milestones |
| `_tech-roadmap.md` | Cuándo entran tecnologías (Polly en M2, pgvector en M3, Marten en M2/M3) |
| `especificacion_sistema_integral_unificado` (Drive) | Visión funcional detallada de cada módulo — fuente para escribir los SPECs |
| `Modelo-Modulos-Campos-MakaERP-v2` (Drive) | Campos concretos por módulo — fuente para el modelo de datos de cada SPEC |
| `effi_erp_documentacion_*` (Drive) | Estado actual de TecnoImportaciones — referencia para M1 y M2 |

---

## Historial de decisiones de milestones

| Fecha | Decisión | Razón |
|---|---|---|
| jun 2026 | Cotizaciones se mueve a M1 | Flujo comercial B2B principal de TecnoImportaciones. Sin cotizaciones no hay OV. |
| jun 2026 | Mensajería IA queda en M2 | Depende de Catálogo estable + CRM + WhatsApp API homologada. No es "básica". |
| jun 2026 | Nómina al final de M2 | 24 empleados, no urgente. Debe construirse sobre Contabilidad funcionando. |
| jun 2026 | WMS en M2 | Útil pero no bloqueante para M1. El inventario básico (stock por bodega) cubre M1. |
