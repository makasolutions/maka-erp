# Profundización por Zonas — Modelado de Módulos PICE / Maka ERP

> **Estado:** Documento vivo de referencia de modelado.
> **Rol:** Captura el detalle de entidades, campos y reglas de negocio de cada módulo, extraído del
> barrido de investigación (NotebookLM + referentes). Es el insumo del que arranca cada SPEC detallado
> cuando se construye el módulo (JIT). NO reemplaza al SPEC de implementación; lo alimenta.
> **Última actualización:** 2026-06-19 (rev. correcciones de doctrina verificadas contra el repo)
> ⚠️ Marcas **[REAL]** / **[DISEÑO]** según §5 del mapa de arquitectura. Eventos casi todos [DISEÑO].

---

## Zona A — Backbone transaccional (Catálogo · Inventario · Órdenes)

### Catálogo
- **Entidades:** `Product` (agregado raíz: SKU único, nombre, descripción HTML, precio base COP, marca,
  categoría), `ProductVariation` (hija; SKU propio, precio propio, stock independiente),
  `PriceList`/`PriceListItem` (tarifas: Base, Descuento, B2B, Volumen, VIP), `Specs` (JSONB flexible por categoría).
- **Reglas:** unicidad estricta de SKU por tenant; validación de esquema JSON para Specs; generación de
  EAN/QR para rastreo físico; sincronización bidireccional con WooCommerce (REST API).
- **Tipos de artículo (de Effi):** materia prima, producto en proceso, terminado, servicio, propiedad/planta,
  maquinaria, herramienta/repuesto, combo, cuenta contable, flete/transporte.
- **Depende de:** Lookups (categorías, marcas, unidades); Parties (proveedores exclusivos: DZOFilm, Nanlite).
- **Eventos:** publica `ProductSyncedToWooCommerce` **[DISEÑO]**.

### Inventario
- **Entidades:** `StockRecord` (por SKU+variante+bodega: Bogotá, Medellín), `SerialNumber` (hija de Product;
  un ID por unidad física; se asigna al ingresar, se vincula a venta y garantía),
  `StockMovement` (ledger inmutable: entradas, salidas, transferencias, bajas),
  `StockReservation` (separa stock de pedidos pendientes).
- **Reglas:** costeo por **promedio ponderado** (actualizado en cada entrada) e **identificación específica**
  (productos con serial); **UEPS eliminado**; `StockAvailable = StockOnHand - StockReserved`;
  transferencias con estado `InTransit` hasta confirmación de recepción; no hay salida sin movimiento
  registrado (trazabilidad total); ajustes por conteo requieren aprobación.
- **Depende de:** Catálogo (SKUs); Parties (bodegas = sedes).
- **Eventos:** publica `StockMoved` **[DISEÑO]** (→ Contabilidad), `StockBelowMinimum` **[DISEÑO]** (→ Compras).
  Consume `OrderPaid` **[DISEÑO]** (decrementa físico), `ImportReceived` **[DISEÑO]** (incrementa con costo real).

### Órdenes / OMS
- **Entidades:** `SalesOrder` (estados: Draft, Confirmada, En Despacho, Entregada),
  `Quotation` (vigencia TTL 24-48h, versionamiento `AmendedFrom`), `DeliveryNote` (remisión).
- **Reglas:** al crear orden pendiente, **reserva** stock automáticamente (evita sobreventa); el
  **descuento físico** (OnHand) solo tras `OrderPaid`; inmutabilidad post-facturación (cambios = nota
  crédito/débito); verificación de **cupo de crédito** en tiempo real (B2B) + validación de **hold operativo**.
- **Depende de:** Parties (clientes, crédito), Catálogo (productos/precios), NamingSeries (consecutivos DIAN).
- **Eventos:** publica `OrderPaid`, `OrderReadyToShip`, `AbandonedCart` **[todos DISEÑO]**.
  Consume `PaymentConfirmed` de pasarelas (Wompi/PayU) **[DISEÑO]**.

---

## Zona B — Comercial-relacional (CRM · Mensajería · Chat)

### CRM (dimensión sobre Party, no entidad separada)
> **Modelado clave:** nuestro Party unificado + Profiles ya es superior al split Lead/Contact/Account.
> El CRM agrega 3 dimensiones sobre el Party: (1) lifecycle/pipeline, (2) actividades/timeline, (3) atribución.

- **Lifecycle (vive en el Party):** `LifecycleStage` enum (Subscriber → Lead → MQL → SQL → Opportunity →
  Customer → Recurrent → Inactive), `LeadScore` (decimal), `MarketingSourceId` (FK origen),
  `ResponsibleUserId` (FK vendedor asignado).
- **Oportunidad = Cotización (vive en su entidad, NO en el Party):** título, valor estimado, probabilidad
  de cierre %, fecha estimada, `WorkflowState` (etapa del embudo). Un tercero puede tener **múltiples
  oportunidades en distintos embudos** simultáneamente. **NO hay selector de embudo en la ficha del Party**;
  la sección CRM lista las oportunidades vigentes.
- **Pipeline (patrón WorkflowDefinition):** embudos dinámicos configurables por tenant. Etapas con
  metadatos: probabilidad de cierre (→ forecast), tiempo de estancamiento (→ alertas), reglas de transición.
- **Timeline (`PartyActivity`, patrón Pipedrive/Mercately):** tipo (Llamada, Email, WhatsApp, Nota, Tarea),
  fecha UTC, autor, resumen, link a documento. "Read-mostly"; se alimenta de eventos.
- **Atribución (GTM/Ads):** Source, Medium, Campaign, Content/Term (vía UTM/GA4). Habilita ROAS conversacional.
- **Custom fields (emula Attio):** tabla `CustomFieldDefinition` (qué campos, tipo, obligatoriedad por acción)
  + valores en JSONB del registro. Permite segmentación y filtros dinámicos.

### Mensajería omnicanal (WhatsApp)
- **Entidades:** `Conversation` (thread unificado WhatsApp/IG/FB), `Message` (contenido, tipo Texto/Media/HSM,
  dirección In/Out, timestamp), `WhatsAppConsent` (value object: opt-in, fecha, origen — obligatorio Meta).
- **Capacidades:** multiagente (Meta Cloud API, bandeja única, asignación round-robin/carga); **in-chat
  commerce** (consultar stock y enviar ficha/link de pago sin salir del chat — inspirado en Mercately);
  plantillas **HSM** (pre-aprobadas Meta); **opt-in** bloquea envíos proactivos sin consentimiento.
- **Referencias de modelado:** Bitrix24 Open Channels, Mercately (chat↔inventario), Kommo.
- **Eventos:** `WhatsAppMessageReceived` **[DISEÑO]** (→ CRM asigna agente, actualiza leadScore).

### Chat / Notificaciones ✅
- **Estado [REAL]:** el flujo `MentionedInChannel` → Notifications (persiste + push SignalR) **funciona
  end-to-end hoy** (verificado en el as-built de Plataforma, 2026-06-19). Es el único flujo de negocio
  cross-módulo operativo. Sirve de referencia del patrón correcto para los demás.
- **Real-time:** el push usa **SignalR** (`AppHub`, grupo `user:{id}`). Recordar: SignalR (bidireccional)
  y SSE (unidireccional) **conviven**; este flujo es push del servidor → cliente sobre SignalR.

---

## Zona C — Operación y finanzas (Compras · Importaciones · Contabilidad · Garantías)

### Compras
- **Entidades:** `SolicitudCompra` (ítem, cantidad, fecha requerida, centro de costos),
  `OrdenCompra` (proveedor, términos, ítems, precios, impuestos/retenciones),
  `RemisionEntrada/GRN` (vínculo a OC, cantidades recibidas, bodega, estado).
- **Reglas:** **validación a tres bandas** (OC = Recepción = Factura proveedor para pagar);
  **vigencia de precios** (prioriza menor precio en última vigencia; al insertar nuevo, el anterior se cierra
  si `ValidUntil` es null); aprobación jerárquica por monto.
- **Depende de Parties:** `SupplierProfile` (moneda, términos, lead time, cuenta bancaria), `FiscalData` (retenciones).
- **Eventos:** publica `PurchaseReceived` **[DISEÑO]**; consume `StockBelowMinimum` **[DISEÑO]**.

### Importaciones
- **Entidades:** `OrdenImportacion` (proveedor extranjero, Incoterm FOB/CIF, país, moneda, subpartida),
  `SimuladorLandedCost` (FOB, flete, seguro, aranceles, IVA importación, bodegaje, aduana, transporte
  nacional, gastos financieros), documentos (factura comercial, packing list, BL/AWB, certificado origen,
  liquidación DIAN).
- **Reglas:** **TRM inmutable** (grabada al cotizar/emitir, API Banco de la República, no cambia
  retroactivamente); distribución de costo nacionalizado proporcional a FOB o peso/volumen; estados de
  tránsito (Pedido → En tránsito → En puerto → En aduana → Nacionalizado → En bodega).
- **Eventos:** publica `ImportReceived` **[DISEÑO]** (actualiza inventario con costo real por unidad).

### Contabilidad NIIF
- **Entidades:** `PlanCuentas/PUC` (NIIF pymes, clases 1-9), `ComprobanteContable` (tipo, fecha, moneda, TRM,
  movimientos: cuenta + tercero + centro de costos + débito/crédito), `PeriodoContable` (mes/año, estado abierto/cerrado).
- **Reglas:** **100% de transacciones generan asiento automático** (sin intervención manual); valoración
  PEPS/identificación específica (UEPS eliminado); multimoneda COP base + diferencia en cambio automática.
- **Es el mayor consumidor de eventos [todos DISEÑO]:** `OrderPaid` (Db banco/Cr ingresos),
  `OrderShipped` (Db costo ventas/Cr inventario), `PurchaseReceived` (Db inventario/Cr CxP),
  `ImportReceived` (Db inventario landed/Cr gastos importación), `WarrantyResolved` (Db gasto/Cr inventario).
- **Depende de Parties:** datos fiscales (exógena, retenciones), `CreditAccount` (límites en tiempo real).

### Garantías
- **Entidades:** `CasoGarantia/RMA` (cliente, descripción falla, fotos/videos, estado),
  `SerialNumber` (vincula unidad vendida con el caso).
- **Reglas:** **vínculo por serial** (valida venta propia + periodo de marca: Canon 1 año, Godox 2 años);
  flujo Diagnóstico → Decisión (Reparar / Reponer stock / Reembolso); reclamación a fabricante
  (`WarrantyClaim` **[DISEÑO]** si falla de fábrica).
- **Eventos:** publica `WarrantyResolved` **[DISEÑO]**; consume `OrderDelivered` **[DISEÑO]** (la entrega
  marca inicio del periodo de garantía).

---

## Zona D — Canales y transversales

### WooCommerce
Sincronización bidireccional de stock/precios/disponibilidad vía REST API. Evento
`ProductSyncedToWooCommerce` **[DISEÑO]**. *(Nota: la doc vieja decía "vía MassTransit" — **MassTransit
nunca estuvo en el código**; el eventing es Wolverine.)*

### Portal B2B
Acceso por invitación con aprobación manual; catálogo con precios diferenciados; conversión automática de OC
del cliente en pedidos; límites de crédito con alertas; historial completo. **Fase 4.**

### Convenios / Marketplace / Stock en vivo
Red comercial colaborativa: tenants venden inventario privado o compartido; stock compartido visible para
terceros autorizados, sincronizado en tiempo real (stock, precios, tiempos de entrega). Convenios = contratos
digitales con firma electrónica (descuentos, crédito, condiciones). *(En el repo as-built: `Agreement` /
`AgreementRule`.)*

### Dropshipping
Identifica automáticamente pedido propio vs dropshipping; notifica al proveedor (API/email); portal del
proveedor (confirma despacho, ingresa guía); liquidación periódica automática (monto menos comisión). **Fase 4.**

### Facturación DIAN electrónica
Documentos: Factura de Venta, Nota Crédito, Nota Débito, Documento Equivalente (POS), Nómina Electrónica.
Transmisión vía API de Alegra. Resoluciones con rangos de numeración y vigencias; alerta al 80% de uso.

---

## Zona E — Módulos transversales adicionales (mapeados, aún no en imagen original)

| Módulo | Entidades clave | Reglas / lógica | Dependencias |
|---|---|---|---|
| **Ventas con Forecast** | Quotation, SalesOrder, BI_Metric | predicción 30 días por pipeline + tasa conversión | Catálogo, Órdenes, Parties |
| **Inventario Predictivo** | StockMovement, Product, Warehouse | días de cobertura, predicción de agotamiento por velocidad | Catálogo, Inventario |
| **Cargas Masivas** | ImportJob, MappingConfig | CSV con mapeo configurable + deduplicación | Catálogo, Parties |
| **Marketing** | Campania, MarketingSource, Lead | segmentación por PartyTag, nutrición por comportamiento | CRM, WhatsApp, Órdenes |
| **Plantillas Mensajería** | HSM_Template, EmailTemplate | plantillas Meta (pedidos, guías, garantías) | WhatsApp API, Órdenes |
| **Automatizaciones** | Trigger, Condition, Action | motor visual sin código (Si-Entonces); **Hangfire** recurrentes, **Wolverine** con estado | (todos los eventos del bus) |
| **Facturación SaaS** | Tenant, SubscriptionPlan, Invoice | cobro recurrente, onboarding self-service | Contabilidad, Billing |

> ⚠️ La doc vieja decía "Automatizaciones: MassTransit Sagas" — **obsoleto**. **MassTransit nunca estuvo
> en el código.** Es Wolverine para flujos con estado, Hangfire para jobs recurrentes.

---

## Patrones técnicos transversales

- **DocStatus / ISubmittable (patrón Frappe, ADR-0006):** documentos fiscales/transaccionales siguen
  Draft → Submitted (inmutable post-firma) → Cancelled. Un Submitted no se edita; se anula y se genera nuevo
  (`AmendedFrom`).
- **NamingSeries (patrón Frappe, ADR-0007):** consecutivos configurables por tenant (prefijos, rangos,
  vigencias DIAN); alerta al 80% del rango autorizado.
- **Controles genéricos polimórficos:** `OwnerType` (string) + `OwnerId` (Guid); cada fila con Activo +
  Principal (índice parcial único) + clasificación por Tabla Básica. N filas por owner. Reutilizables
  cross-módulo (Address, Phone, Contact, SocialMedia, Document, BankAccount, Task).
- **Validación en 3 capas:** Cliente (Syncfusion, máscaras, tiempo real) · Servidor (FluentValidation,
  completitud gobernada) · Dominio (invariantes de agregado) + Persistencia (índices únicos parciales).
- **Completitud gobernada:** el sistema persigue la completitud, no la exige al crear. Valida por acción
  (crear contacto básico permitido; facturar exige identificación + email).
