# Mapa de Arquitectura y Acoplamiento de Módulos — PICE / Maka ERP

> **Estado:** Documento vivo de arquitectura de alto nivel.
> **Rol:** Fuente de verdad del *panorama* del sistema. NO es un SPEC de implementación.
> Los SPECs detallados de cada módulo se escriben just-in-time (JIT) al construir ese módulo,
> y se diseñan consultando este mapa para saber qué módulos vecinos contemplar.
> **Última actualización:** 2026-06-19 (rev. correcciones de doctrina verificadas contra el repo)
> **Autoría:** Arquitectura (consolidado desde investigación de referentes + estado real del repo).

---

## 0. Cómo leer y mantener este documento

Este mapa existe para una cosa: **que cada módulo se diseñe con visión de sus vecinos**, no aislado.
Cuando vayas a diseñar un módulo, mirá su fila en el grafo de acoplamiento (§3), identificá sus
vecinos fuertes, y diseñá sus campos/contratos contemplando **qué van a necesitar esos vecinos de él**.

**Principio rector del diseño acoplado — "exponer, no absorber":**
del barrido de vecinos se extrae *qué campos de este módulo van a ser consumidos por otros* (esos se
incluyen) y *qué pertenece a los otros módulos y solo referencia a este* (eso NO se absorbe; vive en su
módulo). Parties expone `CreditAccount`; la lógica de crédito vive en Órdenes. Parties expone datos
fiscales; la facturación vive en DIAN.

> ⚠️ **Distinción crítica entre diseño y estado real.** Este documento marca cada evento e integración
> como **[REAL]** (existe y funciona hoy en el repo, verificado) o **[DISEÑO]** (planificado, aún no
> implementado). No confundir el roadmap con el código. Ver §5.

---

## 1. Doctrina técnica vigente (CORRECCIONES respecto a documentación anterior)

> Esta sección corrige doctrina obsoleta que aparece en documentos viejos del proyecto (y en NotebookLM).
> **Lo que sigue es lo vigente, verificado en el repo a 2026-06-19** (as-built de Plataforma, ADR-0004,
> `.agents/rules/eventing.md`, `AGENTS.md`).

| Tema | ❌ Doctrina vieja (obsoleta) | ✅ Doctrina vigente (verificada) |
|---|---|---|
| Bus de mensajería / integration events | "MassTransit 8.5.7" / "MassTransit Sagas" | **Wolverine** (bus de integration events cross-módulo; entrega local in-process + **outbox transaccional por módulo** con respaldo Postgres). **MassTransit NUNCA estuvo en el código** (grep = 0, verificado en el as-built de Plataforma y ADR-0004) — era doctrina falsa en CLAUDE/AGENTS viejos. ⚠️ `AGENTS.md` **todavía** lo lista como dependencia del stack (golden rule #13 + tabla) — es **residuo a corregir**, NO la verdad. `.agents/rules/eventing.md` es la fuente vigente del eventing. |
| Mediador CQRS | MediatR | **Mediator source-generated** (el del boilerplate FSH). Wolverine **no** lo reemplaza: conviven en universos disjuntos (Mediator = comandos/queries in-process; Wolverine = integration events cross-módulo). |
| Bus de eventos propio (legacy) | "es la vía de integración" | **En retiro vía migración Wolverine de 5 fases** (Fase 3 cerrada; **Fase 5 borra el bus propio**: `RabbitMqEventBus` + `IEventBus` + outbox/inbox custom, ~880 LOC). El bus legacy operaba; se reemplaza por entrega local Wolverine. Estado transicional documentado en `docs/specs/platform/`. |
| Transporte de eventos internos | RabbitMQ obligatorio | **Entrega local in-process** (monolito modular): *local durable queues* de Wolverine con respaldo Postgres. NO usar `PublishMessage<T>().ToRabbitExchange(...)` salvo que un consumidor **externo real** (otro proceso) deba recibir el evento. Dev y tests **no necesitan broker**. |
| Jobs recurrentes | (varía) | **Hangfire** (del boilerplate FSH) para jobs recurrentes / fire-and-forget; **Wolverine** para flujos con estado. |
| Real-time | "SignalR reemplaza a SSE" (o viceversa) | **SignalR y SSE CONVIVEN — uno NO reemplaza al otro.** **SignalR** (`AppHub` en `/api/v1/realtime/hub`) para bidireccional/colaborativo (chat, presencia, push de notificaciones). **SSE** (handshake de token en dos pasos) para unidireccional. Regla CLAUDE.md: flujo unidireccional → SSE; bidireccional/colaborativo → SignalR. Ver `.agents/rules/realtime.md`. |
| Frontend | "Blazor / MudBlazor → Syncfusion Blazor" | **React 19 + Vite 7 + TypeScript** (NO Blazor). **DOS apps:** `clients/admin` (operador, :5173) y `clients/dashboard` (tenant, :5174). TanStack Query v5, React Router 7, Radix + Tailwind v4 + CVA. **Syncfusion 33.x SOLO vía wrappers Maka*** (`MakaGrid`/`MakaChart`/`MakaKanban`/`MakaPivot`/`MakaScheduler`). i18next ES/EN obligatorio (`t()`, ES primero). ⚠️ `CLAUDE.md §6` (Fase 0) todavía describe purga de MudBlazor + setup Syncfusion **Blazor** — **obsoleto**, es residuo del plan inicial. |
| Persistencia | (Marten evaluado) | **EF Core 10** sobre PostgreSQL 17. Marten evaluado y descartado para el ERP relacional. |
| Multitenancy | — | **Finbuckle, shared-database** (un solo DB, discriminador `TenantId` + global query filters EF, default-ON vía `BaseDbContext`). Opt-out solo vía `IGlobalEntity`. |
| Métodos de costeo inventario | UEPS incluido | **UEPS ELIMINADO** (NIIF). Solo PEPS, promedio ponderado e identificación específica. |

**Patrón de handlers de integration events (vigente, `eventing.md` + migración Wolverine):**
- **Discovery (footgun):** la discovery por convención NO descubre handlers con `DisableConventionalDiscovery`
  activo. Cada handler se registra a mano con `opts.Discovery.IncludeType(typeof(MiHandler))` en
  `Program.cs`. Si se olvida, el handler NO corre y **no hay error de compilación**.
- **Tenant en consumidores:** restaurado de forma **estructural** por `TenantContextMiddleware` (incoming),
  que instala `envelope.TenantId` vía `IMultiTenantContextSetter` **antes** de invocar el handler (cerrado
  en Fase 3 — INV-9 estructural, ya no es regla violable). Para escritura tenant-aislada fuera de ese
  pipeline, usar el helper `TenantScopedDbContext` + `IServiceScopeFactory` (Wolverine prohíbe service
  location con `IServiceProvider` en handlers).

---

## 2. Los tres anillos de centralidad

El sistema se organiza en tres anillos según cuántos módulos dependen de cada uno. Esto determina
el orden de construcción (núcleo primero) y la estrategia de empaquetado comercial (§6).

### Anillo NÚCLEO — de ellos depende casi todo
Cambiar un campo aquí repercute en muchos módulos. Se construyen y estabilizan primero.

- **Multitenancy** (Finbuckle) — inyecta `TenantId` en cada query y registro.
- **Identidad** — usuario autenticado, JWT, roles, permisos.
- **Parties / Terceros** — agregado raíz de identidad: clientes, proveedores, empleados, contactos.
- **Catálogo** — define qué se vende y a qué precio base.
- **Lookups / Tablas Básicas** — clasificaciones para los controles genéricos (bancos, tipos, industrias).
- **NamingSeries** — consecutivos legales/transaccionales (resoluciones DIAN) para todos los documentos.
- **Auditoría** — request/response/security/exception (del boilerplate).
- **Archivos** — almacenamiento (S3/MinIO), usado por varios módulos.

### Anillo PROCESO — consumen núcleo, alimentan hojas
El corazón operativo. Donde ocurren las transacciones de negocio.

- **Órdenes / OMS** — orquesta el ciclo de venta (Cotización → Orden → Despacho).
- **Inventario** — existencia física, reservas, movimientos, costeo.
- **Compras** — abastecimiento nacional.
- **Importaciones** — abastecimiento internacional (landed cost, TRM).
- **CRM** — adquisición, oportunidades, pipeline, atribución.
- **WhatsApp / Mensajería** — diálogo omnicanal.
- **Logística** — transportadoras, guías, contraentrega.
- **RRHH** — empleados (mínimo para M1: vendedor/cobrador).

### Anillo HOJA — consumen de otros, rara vez alimentan procesos core
Documentos finales, registros, capas analíticas. Dependen de los anteriores.

- **Facturación DIAN** — documento legal final que deriva de una orden pagada.
- **Contabilidad NIIF** — registro económico final (mayor consumidor de eventos).
- **Garantías** — postventa, vínculo por número de serie.
- **BI** — solo lectura, dashboards.
- **IA** — capa predictiva (pLTV, resúmenes, scoring).
- **Chat / Notificaciones** — entrega de avisos internos (✅ operativo hoy).

### Canales y transversales
- **WooCommerce** — sincronización con la tienda online.
- **Portal B2B**, **Convenios/Marketplace**, **Stock en vivo**, **Dropshipping** — colaboración y canales.
- **Marketing**, **Plantillas**, **Automatizaciones**, **Cargas Masivas** — transversales.
- **Facturación SaaS** — billing de suscripciones del propio producto a sus tenants (meta-nivel).

---

## 3. Grafo de acoplamiento: qué expone Parties a cada vecino

Parties es el núcleo del que más módulos dependen. Esta tabla es la referencia para diseñar Parties
y sus controles **contemplando lo que cada vecino va a consumir** (principio "exponer, no absorber").

| Módulo consumidor | Qué consume de Parties | Evento (estado) |
|---|---|---|
| **Facturación DIAN** | `FiscalData`: NIT, DV (algoritmo DIAN), régimen tributario, responsabilidades, EmailFacturación | `OrderPaid` **[DISEÑO]** → genera factura |
| **Órdenes / OMS** | `CreditAccount`: cupo, saldo disponible, días crédito, forma de pago + `DefaultBranchId` | `OrderPaid` **[DISEÑO]** → actualiza saldo |
| **CRM** | `LifecycleStage`, `LeadScore`, `MarketingSourceId` (atribución), `tags`, `ResponsibleUserId` | `PartyCreated` **[DISEÑO]** → inicia seguimiento |
| **Logística** | `AddressList`: direcciones con código DANE (MunicipioId) + geolocalización | `OrderReadyToShip` **[DISEÑO]** → genera guía |
| **WhatsApp / Mensajería** | número WhatsApp, WhatsAppName, **Opt-in/Consentimiento** (cumplimiento Meta) | `MessageReceived` **[DISEÑO]** → vincula por teléfono |
| **Compras / Importaciones** | `SupplierProfile`: clasificación, moneda (COP/USD), términos de pago, Lead Time, cuenta bancaria | `SupplierProfileActivated` **[DISEÑO]** → habilita OC |
| **Contabilidad NIIF** | identificación fiscal + nombre (auxiliares por tercero, exógena, retenciones) | `JournalEntryCreated` **[DISEÑO]** → ancla asiento al tercero |
| **RRHH** | `EmployeeProfile`: código, cargo, jefe inmediato, sucursal | `EmployeeHired` **[DISEÑO]** → flujos de nómina |

**Delegación jerárquica (patrón Odoo):** si `ParentPartyId` no es null, `FiscalData` (NIT del grupo) y
`CreditAccount` (cupo del grupo) pueden resolverse desde la matriz vía `ResolveCommercialEntity()`.

---

## 4. Acoplamientos fuertes entre módulos de proceso

Más allá de Parties, estos son los pares con acoplamiento fuerte (a contemplar al diseñar cualquiera de ellos):

- **Órdenes ↔ Inventario:** Órdenes solicita **reserva** de stock al confirmar; el **descuento físico**
  (OnHand) ocurre solo tras confirmación de pago. `StockAvailable = StockOnHand - StockReserved`.
- **Inventario ↔ Catálogo:** Inventario rastrea existencias por SKU/variante definidos en Catálogo;
  consume `SerialNumber` para trazabilidad de unidades (vital: cámaras, drones).
- **Órdenes ↔ Catálogo:** OMS lee tarifas (Base/Descuento/B2B/Volumen/VIP) según el perfil del tercero.
- **Compras → Inventario + Contabilidad:** `PurchaseReceived` **[DISEÑO]** actualiza stock y genera asiento.
- **Importaciones → Inventario:** `ImportReceived` **[DISEÑO]** incrementa stock con costo nacionalizado real.
- **Contabilidad ← (casi todos):** es el **mayor consumidor de eventos** — cada transacción genera asiento
  automático. Consume `OrderPaid`, `OrderShipped`, `PurchaseReceived`, `ImportReceived`, `WarrantyResolved`.
- **Garantías ↔ Inventario/Órdenes:** vínculo por `SerialNumber`; valida que el serial corresponda a venta
  propia y esté en periodo de marca (Canon 1 año, Godox 2 años).
- **Automatización ← (todos):** escucha eventos de bajo nivel (`StockBelowMinimum` → solicitud de compra;
  `LeadScore` → asignación de vendedor; `AbandonedCart` → WhatsApp + email).
- **WhatsApp ↔ CRM:** `WhatsAppMessageReceived` **[DISEÑO]** asigna agente y actualiza leadScore.

---

## 5. Eventos de integración: REAL vs DISEÑO

> Esta es la sección más importante para no construir sobre ficción.

### ✅ [REAL] — existen y entregan end-to-end hoy (verificado 2026-06-19, as-built Plataforma)
Estos 4 son los únicos integration events que funcionan de verdad hoy:

| Evento | Publica | Consume | Acción |
|---|---|---|---|
| `UserRegisteredIntegrationEvent` | Identity | Webhooks | fanout a suscripción |
| `TokenGeneratedIntegrationEvent` | Identity | Webhooks | log |
| `FileFinalizedIntegrationEvent` | Files | Webhooks | fanout a suscripción |
| `MentionedInChannelIntegrationEvent` | Chat | Notifications | persiste notificación + push SignalR |

> El flujo `MentionedInChannel` → Notifications (persiste + push SignalR a `user:{id}`) es el único
> flujo de negocio cross-módulo operativo. Sirve de **referencia del patrón correcto** para los demás.

### 🔶 [DISEÑO] — planificados, NO implementados aún
Todos los eventos de negocio del roadmap. Existen como diseño, no como código:
`PartyCreated`, `CustomerProfileActivated`, `SupplierProfileActivated`, `HoldPlaced`, `CreditExhausted`,
`OrderPaid`, `OrderShipped`, `OrderReadyToShip`, `OrderDelivered`, `AbandonedCart`, `PurchaseReceived`,
`ImportReceived`, `StockMoved`, `StockBelowMinimum`, `WarrantyResolved`, `WarrantyClaim`,
`QuotationSubmitted`, `JournalEntryCreated`, `EmployeeHired`, `ProductSyncedToWooCommerce`,
`PaymentConfirmed`, `SubscriptionRenewed`, `TenantCreated`, `DataImported`, `LeadCaptured`.

> Al implementar cada uno: solo cablear el publish cuando exista el consumidor real (lección del eventing:
> publicar a un consumidor inexistente = bug silencioso). Diseñar los módulos *preparados* para emitirlos,
> sin emitir hacia el vacío.

---

## 6. Venta por módulos — análisis técnico (decisión comercial pendiente del CEO)

> Esto es **análisis técnico de separabilidad**, no la decisión de paquetes. Los planes comerciales los
> define Juan. Aquí solo se establece qué es técnicamente separable y qué arrastra dependencias.

### Regla de separabilidad
- Un módulo **HOJA** es más fácil de vender suelto (consume, no es consumido).
- Un módulo **NÚCLEO** nunca se vende suelto (todo depende de él) — es base obligatoria.
- Un módulo **PROCESO** arrastra a sus vecinos fuertes.

### Base mínima obligatoria (siempre presente, no vendible suelta)
**Multitenancy + Identidad + Parties + Lookups + NamingSeries + Auditoría + Archivos.**
Es el sustrato. Cualquier paquete lo incluye.

### Núcleo operativo (reemplaza Effi — el paquete "ERP comercio")
Base mínima + **Catálogo + Inventario + Órdenes/OMS + Facturación DIAN + Logística**.
Es el paquete más pequeño que constituye un ERP de comercio funcional.

### Paquetes candidatos (a validar comercialmente)
- **Contador / Contabilidad multiempresa:** Base mínima + Contabilidad NIIF + Facturación DIAN.
  *Tensión técnica:* Contabilidad consume eventos de los módulos transaccionales. Vendido solo para
  contabilidad, necesitaría entrada manual de asientos o importación — viable pero limita el valor.
- **Solo Mensajería / Bots:** Base mínima + WhatsApp + CRM (mínimo) + Plantillas + Automatizaciones.
  Separable razonablemente; el CRM mínimo es la dependencia.
- **Solo CRM:** Base mínima + CRM + Mensajería. La oportunidad = Cotización, así que arrastra una versión
  ligera de Órdenes (la Cotización). A evaluar.
- **Solo Importaciones:** Base mínima + Importaciones + Inventario (para recibir el stock) + Compras.
  Importaciones sin Inventario no tiene dónde aterrizar el landed cost.

> **Conclusión técnica para el CEO:** los paquetes "hoja" (Contabilidad, BI) y los de canal (Mensajería)
> son los más separables. CRM e Importaciones arrastran dependencias de proceso que hay que incluir en el
> paquete. La base mínima va en todos. Los **Feature Flags por tenant** (ya contemplados en la arquitectura
> SaaS) son el mecanismo técnico para activar/desactivar módulos por cliente.

---

## 7. Milestones (contexto de secuencia)

- **M0 — Core:** Plataforma + datos maestros. Eventing en migración Wolverine (Fase 3 cerrada). Parties v2 **completo**.
- **M1 — MVP Interno (reemplaza Effi):** Catálogo, Cotizaciones, Órdenes, Inventario, Compras + CRM + Dashboard BI.
- **M2 — Operación completa:** DIAN, Contabilidad, Importaciones, Garantías, Mensajería omnicanal, Nómina.
- **M3 — SaaS multi-cliente:** Feature flags por tenant, pLTV/pgvector, Facturación SaaS, Portal B2B, Marketplace.

---

## 8. Cómo evoluciona este documento

Es un documento vivo. A medida que se construye cada módulo y se descubren acoplamientos no previstos
(o eventos que pasan de [DISEÑO] a [REAL]), se actualiza aquí. Los ciclos de ajuste hacia atrás
(un módulo nuevo revela un campo que falta en Parties) se reflejan primero en este mapa y luego en el
SPEC del módulo afectado. La convergencia hacia el "diccionario de datos definitivo" ocurre por estos
ciclos, no por escribir todos los SPECs por adelantado.
