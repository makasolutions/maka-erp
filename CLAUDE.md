@AGENTS.md

# CLAUDE.md — Maka Omni-Commerce Ecosystem
# Fuente de verdad absoluta. Leer COMPLETO antes de ejecutar cualquier comando.
# Versión: 3.1 | Mayo 2026 — Stack en AGENTS.md; aquí solo reglas de negocio Maka

---

## ⚡ PROTOCOLO DE INICIO — OBLIGATORIO EN CADA SESIÓN

Antes de escribir UNA SOLA línea de código, ejecuta este checklist mentalmente:

1. ¿Leí este archivo completo? → Si no: léelo ahora. Sin excepciones.
2. ¿Estoy en la rama correcta? → `git branch --show-current` (debe ser `develop` o `feature/`)
3. ¿Hay migraciones EF Core pendientes? → `dotnet ef migrations list`
4. ¿La tarea pertenece al módulo correcto? → Ver §7 (requerimientos por fase)
5. ¿La tarea viola alguna REGLA PROHIBIDA? → Ver §9
6. ¿Es tarea nueva? → Activar Plan Mode (`Shift+Tab`), proponer modelo y esperar confirmación de Juan

Si cualquier respuesta es dudosa: **PARAR y preguntar antes de continuar.**

---

## 1. IDENTIDAD DEL PROYECTO

| Campo | Valor |
|---|---|
| **Producto** | Maka Omni-Commerce Ecosystem |
| **Tipo** | Plataforma SaaS: ERP + CRM + WhatsApp Omnicanal |
| **Empresas** | Maka Solutions SAS / Tecnoimportaciones |
| **Líder técnico** | Juan Carlos Sánchez Hernández (CEO / Ing. Sistemas) |
| **Equipo** | 14 personas — Bogotá y Medellín |
| **Negocio** | Retail B2B/B2C audiovisual profesional + Maka Studios |
| **Marcas clave** | Sony, DJI, Canon, Nikon, Blackmagic, Nanlite, Godox, DZOFilm (exclusivo Colombia) |
| **ERP a reemplazar** | Effi / Efficommerce |
| **E-commerce** | WooCommerce + Electro Theme (se mantiene; integración vía API) |

### Usuarios clave

| Perfil | Nombre | Módulos |
|---|---|---|
| CEO / Admin | Juan | Todos |
| CFO | Yuly | Contabilidad, Facturación, BI |
| Gerente Medellín | Sandra | Inventario, Ventas, Logística |
| Contabilidad | Daniela | Contabilidad, Facturación DIAN |
| Dir. Creativo | Juan Daniel | CRM, Mensajería |

---

## 2. VISIÓN DEL SISTEMA

> Un único flujo de datos donde cada acción comercial —contacto, cotización, pedido, pago, despacho, entrega, garantía— actualiza en tiempo real todos los módulos relevantes sin intervención manual. La IA asiste a cada actor con el contexto preciso en el momento preciso.

Un solo registro viaja sin interrupciones desde el primer Lead hasta la resolución de garantía. **No hay sincronizaciones entre sistemas: hay un sistema.**

---

## 6. FASE 0 — CONFIGURACIÓN DEL AMBIENTE DE DESARROLLO

**Objetivo:** Entorno 100% funcional, MudBlazor eliminado, Syncfusion instalado.
**Criterio de éxito:** `dotnet build` sin errores, API inicia, Scalar accesible, sin referencias MudBlazor.

### PASO 0.1 — Verificar prerequisitos

```bash
dotnet --version          # Requerido: 10.0.x
node --version            # Requerido: 22.x LTS
docker --version
docker compose version    # v2.x (sin guión)
git --version
dotnet ef --version       # Si falta: dotnet tool install --global dotnet-ef
```

### PASO 0.2 — Repositorio (ya hecho)

```bash
# Verificar que origin y upstream están configurados
git remote -v
# Debe mostrar: origin (tu fork) + upstream (fullstackhero)

# Confirmar que estás en develop
git branch --show-current
```

### PASO 0.3 — Docker Compose (infraestructura local)

Crear `docker-compose.yml` en la raíz del proyecto:

```yaml
version: '3.9'
name: maka-local

services:
  postgres:
    image: postgres:16-alpine
    container_name: maka_postgres
    environment:
      POSTGRES_USER: maka_user
      POSTGRES_PASSWORD: maka_dev_2026
      POSTGRES_DB: maka_erp_dev
    ports:
      - "5432:5432"
    volumes:
      - maka_postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U maka_user -d maka_erp_dev"]
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  redis:
    image: redis:7-alpine
    container_name: maka_redis
    ports:
      - "6379:6379"
    volumes:
      - maka_redis_data:/data
    restart: unless-stopped

  rabbitmq:
    image: rabbitmq:3.13-management-alpine
    container_name: maka_rabbitmq
    environment:
      RABBITMQ_DEFAULT_USER: maka_user
      RABBITMQ_DEFAULT_PASS: maka_dev_2026
      RABBITMQ_DEFAULT_VHOST: maka_vhost
    ports:
      - "5672:5672"
      - "15672:15672"   # http://localhost:15672
    volumes:
      - maka_rabbitmq_data:/var/lib/rabbitmq
    restart: unless-stopped

  adminer:
    image: adminer:latest
    container_name: maka_adminer
    ports:
      - "8081:8080"     # http://localhost:8081
    restart: unless-stopped

volumes:
  maka_postgres_data:
  maka_redis_data:
  maka_rabbitmq_data:
```

```bash
docker compose up -d
docker compose ps   # todos deben estar "Up" o "healthy"
```

### PASO 0.4 — Configurar appsettings.Development.json

En `src/Host/FSH.Starter.Api/appsettings.Development.json` (este archivo está en .gitignore):

```json
{
  "DatabaseOptions": {
    "ConnectionString": "Host=localhost;Port=5432;Database=maka_erp_dev;Username=maka_user;Password=maka_dev_2026"
  },
  "RedisOptions": {
    "ConnectionString": "localhost:6379,abortConnect=false"
  },
  "RabbitMQOptions": {
    "Host": "localhost",
    "VirtualHost": "maka_vhost",
    "Username": "maka_user",
    "Password": "maka_dev_2026"
  },
  "JwtOptions": {
    "Key": "CAMBIAR_POR_KEY_DE_AL_MENOS_64_CARACTERES",
    "Issuer": "MakaERP",
    "Audience": "MakaERP",
    "TokenExpirationInMinutes": 60,
    "RefreshTokenExpirationInDays": 7
  },
  "Syncfusion": {
    "LicenseKey": "PENDIENTE"
  },
  "WhatsAppOptions": {
    "VerifyToken": "PENDIENTE",
    "AccessToken": "PENDIENTE",
    "PhoneNumberId": "PENDIENTE",
    "AppSecret": "PENDIENTE",
    "ApiVersion": "v19.0"
  },
  "DianOptions": {
    "PtaProvider": "Alegra",
    "ApiKey": "PENDIENTE",
    "SandboxMode": true
  }
}
```

> ⚠️ Verificar que `appsettings.Development.json` está en el `.gitignore`. Si no: agregar.

### PASO 0.5 — Purga total de MudBlazor ⚠️

```bash
# 1. Detectar todas las referencias — NO eliminar nada aún, solo reportar
echo "=== .csproj con MudBlazor ==="
grep -r "MudBlazor" --include="*.csproj" -l

echo "=== .cs con MudBlazor ==="
grep -r "MudBlazor" --include="*.cs" -l

echo "=== .razor con MudBlazor o componentes Mud* ==="
grep -r "MudBlazor\|Mud[A-Z]" --include="*.razor" -l

# 2. Reportar lista a Juan y esperar confirmación para proceder

# 3. Una vez confirmado: remover paquete de cada .csproj
dotnet remove [PROYECTO].csproj package MudBlazor

# 4. Eliminar @using MudBlazor de _Imports.razor

# 5. Reemplazar componentes Mud* por equivalentes Syncfusion (tabla abajo)

# 6. Verificación final — debe devolver cero resultados
grep -r "MudBlazor\|Mud[A-Z]" --include="*.razor" --include="*.cs" --include="*.csproj" .
```

**Tabla de equivalencias MudBlazor → Syncfusion:**

| MudBlazor | Syncfusion Blazor |
|---|---|
| `MudDataGrid` / `MudTable` | `SfGrid` |
| `MudTextField` | `SfTextBox` |
| `MudSelect` | `SfDropDownList` |
| `MudDatePicker` | `SfDatePicker` |
| `MudDialog` | `SfDialog` |
| `MudButton` | `button` HTML con estilos Syncfusion |
| `MudAlert` | `SfToast` |
| `MudChip` | `SfChip` |
| `MudStepper` | `SfStepWizard` |
| `MudTabs` | `SfTab` |
| `MudNavMenu` | `SfSidebar` + `SfTreeView` |
| `MudProgressLinear` | `SfProgressBar` |
| `MudTooltip` | `SfTooltip` |

### PASO 0.6 — Instalar Syncfusion

```bash
# Instalar en el proyecto Blazor del Host
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Core
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Grids
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Kanban
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Charts
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Inputs
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Popups
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Navigations
dotnet add src/Host/FSH.Starter.Api/FSH.Starter.Api.csproj package Syncfusion.Blazor.Notifications
```

En `Program.cs`:
```csharp
Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(
    builder.Configuration["Syncfusion:LicenseKey"]
    ?? throw new InvalidOperationException("Syncfusion license key not configured"));

builder.Services.AddSyncfusionBlazor();
```

En `_Imports.razor` (agregar, reemplazando las de MudBlazor):
```razor
@using Syncfusion.Blazor
@using Syncfusion.Blazor.Grids
@using Syncfusion.Blazor.Kanban
@using Syncfusion.Blazor.Charts
@using Syncfusion.Blazor.Inputs
@using Syncfusion.Blazor.Popups
@using Syncfusion.Blazor.Navigations
@using Syncfusion.Blazor.DropDowns
@using Syncfusion.Blazor.Notifications
@using Syncfusion.Blazor.Calendars
```

En `App.razor` o `index.html`:
```html
<link href="_content/Syncfusion.Blazor.Themes/bootstrap5.css" rel="stylesheet" />
```

### PASO 0.7 — Actualizar paquetes y compilar

```bash
# Ver qué paquetes están desactualizados
dotnet list src/FSH.Starter.slnx package --outdated

# Build completo
dotnet build src/FSH.Starter.slnx

# Si hay errores: reportar ANTES de intentar corregir nada
```

### PASO 0.8 — Verificación final Fase 0

```bash
# Correr con Aspire (levanta todos los servicios definidos en AppHost)
dotnet run --project src/Host/FSH.Starter.AppHost
```

**Criterios de éxito — todos deben pasar:**
```
✅ dotnet build → sin errores
✅ API inicia sin excepciones
✅ Scalar UI accesible (el endpoint lo define FSH, típicamente /scalar o /api-docs)
✅ Log → "Connected to PostgreSQL"
✅ Log → "Redis connected"
✅ Log → "MassTransit started" / "RabbitMQ connected"
✅ grep -r "MudBlazor" . → cero resultados
✅ http://localhost:15672 → RabbitMQ Management UI accesible
```

**Cuando pasen todos: reportar "✅ FASE 0 COMPLETADA" y esperar instrucción para Fase 1.**

---

## 7. MÓDULOS Y REQUERIMIENTOS FUNCIONALES POR FASE

---

### FASE 1: NÚCLEO OPERATIVO — Reemplazar Effi
**Semanas 1-8 | Criterio de éxito:** E-commerce opera sin Effi.

#### Módulo Catalog — Productos y Catálogo

**RF-CAT-1** Modelo híbrido: datos base tipados (SKU, Nombre, PrecioBase COP) + campo `Specs` de tipo `JsonDocument` (JSONB en PostgreSQL) para especificaciones técnicas variables por categoría.

**RF-CAT-2** Productos simples y variables. Las variantes heredan el SKU padre pero tienen SKU propio, precio propio y stock propio.

**RF-CAT-3** Códigos EAN + generación de etiquetas QR para impresión.

**RF-CAT-4** Precios múltiples: base, descuento, lista B2B, volumen, cliente VIP.

**RF-CAT-5** SEO por producto: meta título, meta descripción, URL amigable, JSON-LD structured data.

**RF-CAT-6** Importación masiva desde CSV con mapeo de columnas configurable.

**RF-CAT-7** Sincronización bidireccional con WooCommerce vía REST API (stock, precios, disponibilidad). Evento de integración `ProductSyncedToWooCommerceEvent` vía MassTransit.

#### Módulo Inventory — Inventario Multibodega

**RF-INV-1** Stock en tiempo real por SKU + variante + bodega. Bodegas iniciales: Bogotá, Medellín.

**RF-INV-2** `SerialNumber` como entidad hija de `Product`. Un S/N por unidad física. Se asigna al ingresar mercancía; se vincula a venta y garantía. Vital para cámaras, drones, equipos audiovisuales.

**RF-INV-3** `StockMovement`: trazabilidad de cada movimiento — entradas, salidas, transferencias, ajustes, devoluciones, bajas, muestras. Cada movimiento publica `StockMovedEvent` → contabilidad genera asiento automático.

**RF-INV-4** Transferencias entre bodegas con estado `InTransit` hasta confirmación de recepción.

**RF-INV-5** `StockReservation`: stock reservado para pedidos pendientes de pago. `StockAvailable = StockOnHand - StockReserved`.

**RF-INV-6** Stock mínimo por producto/bodega. Al alcanzar el mínimo: publicar `StockBelowMinimumEvent` → Automation module crea solicitud de compra + alerta.

**RF-INV-7** Valoración Promedio Ponderado. Costo promedio actualizado en cada entrada. Exposición vía query `GetInventoryValuationQuery`.

**RF-INV-8** Conteos físicos cíclicos con aprobación requerida antes de aplicar ajuste.

**RF-INV-9** Query `GetStockCoverageQuery`: días de cobertura estimados por producto según velocidad de ventas.

#### Módulo Orders — OMS

**RF-ORD-1** Flujo: `Quotation` → `SalesOrder` → `Invoice` → `DeliveryNote`. Cada transición es un Command que publica Integration Event.

**RF-ORD-2** Notificaciones automáticas al cliente en cada cambio de estado: WhatsApp (plantilla HSM) + email.

**RF-ORD-3** Al confirmar pago: publicar `OrderPaidEvent` → Billing genera factura DIAN → Inventory descuenta stock.

**RF-ORD-4** Al preparar despacho: publicar `OrderReadyToShipEvent` → Logistics genera guía transportadora.

**RF-ORD-5** Devoluciones: `ReturnOrder` → reingreso al inventario + generación de nota crédito DIAN.

**RF-ORD-6** Métodos de pago: Wompi, PayU, PSE, Nequi, Daviplata, transferencia, contraentrega.

**RF-ORD-7** Carritos abandonados: `AbandonedCartEvent` a las 2h y 24h → Automation envía WhatsApp + email.

#### Módulo Billing — Facturación Electrónica DIAN

**RF-BILL-1** Documentos: Factura de Venta, Nota Crédito, Nota Débito, Documento Equivalente (POS), Nómina Electrónica.

**RF-BILL-2** Transmisión en tiempo real a PTA. Integración inicial: Alegra API. Re-envío automático si falla.

**RF-BILL-3** IVA diferencial: 0%, 5%, 19%. Motor de impuestos parametrizable por tipo de cliente.

**RF-BILL-4** Retenciones automáticas según tipo de cliente y tabla de retenciones configurada.

**RF-BILL-5** Resoluciones: rangos de numeración, vigencias, alerta al 80% de uso del rango.

**RF-BILL-6** Entrega al cliente: email + WhatsApp (PDF + XML) automático al emitir.

**RF-BILL-7** Información exógena DIAN: formatos 1001, 1003, 1005, 1006, 1007, 1008, 1009. XML compatible con prevalidador DIAN.

#### Módulo Logistics — Transportadoras

**RF-LOG-1** Transportadoras con API oficial: Coordinadora, Servientrega, Interrapidísimo, TCC, Domina, Envía.

**RF-LOG-2** Por transportadora: cotización automática, generación de guía con N° rastreo, tracking en tiempo real, notificación al cliente, reporte de novedades.

**RF-LOG-3** Contraentrega: recaudo registrado al recibir de transportadora, conciliación automática.

**RF-LOG-4** Despacho masivo: cola filtrable, picking list por pedido con ubicación, impresión masiva de etiquetas.

**RF-LOG-5** Calculadora de envío en checkout. Seguro obligatorio para productos de alto valor. Click & collect en tiendas Bogotá/Medellín.

---

### FASE 2: CRM E IMPORTACIONES
**Semanas 9-16 | Criterio de éxito:** Pipeline comercial activo, costos de importación calculados.

#### Módulo CRM

**RF-CRM-1** Ciclo Lead → MQL → SQL → Oportunidad → Cliente. Clasificación automática por reglas.

**RF-CRM-2** Lead scoring por comportamiento: páginas visitadas, productos consultados, interacciones previas.

**RF-CRM-3** Captura desde: WhatsApp (primer mensaje), formularios web, Instagram DM, Facebook Messenger, CSV.

**RF-CRM-4** Pipelines configurables: B2C, B2B, distribuidores, recuperación. Vista Kanban con `SfKanban`.

**RF-CRM-5** Ficha 360° del cliente: datos, historial de compras, conversaciones WhatsApp, garantías activas, facturas, cotizaciones. Una sola pantalla, sin cambiar de módulo.

**RF-CRM-6** Cuentas B2B: empresa con múltiples contactos, límite de crédito, historial de pagos.

**RF-CRM-7** Cotizaciones rápidas con reserva temporal de stock (TTL configurable 24-48h).

**RF-CRM-8** Actividades: llamadas, reuniones (sync Google Calendar), tareas con recordatorios, notas internas.

#### Módulo Imports — Importaciones Internacionales

**RF-IMP-1** Orden de importación: proveedor extranjero, incoterm (FOB/CIF), país, moneda, subpartida arancelaria.

**RF-IMP-2** Simulador de costo de importación: FOB + TRM + flete + seguro + arancel + IVA importación + bodegaje + aduana + transporte nacional + gastos financieros.

**RF-IMP-3** TRM del día (API Banco de la República Colombia) **grabada físicamente** en el registro al cotizar. Inmutable. No puede cambiar retroactivamente.

**RF-IMP-4** Distribución del costo entre los productos del despacho: proporcional a FOB o a peso/volumen, configurable.

**RF-IMP-5** Estados: Pedido → En tránsito → En puerto → En aduana → Nacionalizado → En bodega.

**RF-IMP-6** Carga de documentos: factura comercial, packing list, BL/AWB, certificado de origen, liquidación DIAN.

**RF-IMP-7** Al recepcionar en bodega: publicar `ImportReceivedEvent` → Inventory actualiza stock con costo real por unidad → Accounting genera asiento de importación.

**RF-IMP-8** Proveedores de Tecnoimportaciones: Canon, Sony, DJI, Godox, Nanlite, Blackmagic, DZOFilm (directo China, distribución exclusiva Colombia).

#### Módulo WhatsApp — Omnicanal

**RF-WA-1** Meta Cloud API (oficial). Webhooks para recepción de mensajes en tiempo real vía SSE.

**RF-WA-2** Bandeja multiagente con SSE. Una conversación asignada a un agente. Los demás pueden ver, no escribir.

**RF-WA-3** Panel lateral con ficha CRM del contacto actualizada en tiempo real (SSE stream desde CRM module).

**RF-WA-4** Plantillas HSM: confirmación de pedido, número de guía, notificación de garantía resuelta.

**RF-WA-5** Ciclo de venta dentro del chat: consulta → stock → ficha producto → confirmar → link de pago → `OrderPaidEvent` → OMS.

**RF-WA-6** Bot IA entrenado con catálogo, políticas de envío y garantía, FAQs, tono de marca. Escalado a humano por reglas configurables.

**RF-WA-7** Performance Hub: ROAS conversacional, tasa de conversión por agente, valor promedio por canal.

#### Módulo Purchasing — Compras Nacionales

**RF-PUR-1** Solicitud de compra con aprobación por niveles de monto.

**RF-PUR-2** Cotización a múltiples proveedores con comparativa automática. Priorizar al de menor precio en última vigencia activa.

**RF-PUR-3** Vigencia de precios: precio vigente si `ValidUntil IS NULL`. Al insertar nuevo precio para mismo proveedor/artículo, el anterior se cierra automáticamente con fecha actual.

**RF-PUR-4** Validación a 3 bandas: Orden de Compra + Recepción + Factura del proveedor.

#### Módulo Accounting — Contabilidad

**RF-ACC-1** Plan de cuentas NIIF para pymes (Colombia).

**RF-ACC-2** Asientos automáticos por Integration Events recibidos de otros módulos:

| Evento recibido | Asiento generado |
|---|---|
| `OrderPaidEvent` | Débito caja/banco / Crédito ingresos |
| `OrderShippedEvent` | Débito costo de ventas / Crédito inventario |
| `PurchaseReceivedEvent` | Débito inventario / Crédito cuentas por pagar |
| `ImportReceivedEvent` | Débito inventario costo total / Crédito múltiples gastos |
| `OrderReturnedEvent` | Reversión venta + reingreso inventario |
| `WarrantyResolvedEvent` | Débito gasto garantías / Crédito proveedor o inventario |

**RF-ACC-3** Aging report de cuentas por cobrar y pagar. Conciliación bancaria asistida por IA.

**RF-ACC-4** Múltiples monedas con diferencia en cambio automática (COP base + USD + EUR mínimo).

**RF-ACC-5** Reportes: Balance General, P&L (por período/canal/línea), Flujo de Caja, Estado de Patrimonio.

**RF-ACC-6** Integridad: documentos aprobados/facturados son **inmutables**. Lógica de bloqueo en entidades de dominio.

---

### FASE 3: IA Y AUTOMATIZACIÓN
**Semanas 17-24 | Criterio de éxito:** Bot resuelve >50% de consultas sin intervención humana.

#### Módulo Warranties — Garantías y Postventa

**RF-WAR-1** Garantía registrada al vender, vinculada al `SerialNumber`. Fecha inicio = fecha entrega confirmada por transportadora.

**RF-WAR-2** Períodos por marca (Tecnoimportaciones): Canon 1a, Sony 1a, DJI 12m, Godox 2a, Nanlite 2a, DZOFilm 1a, Blackmagic 1a.

**RF-WAR-3** Apertura de caso desde WhatsApp bot, portal cliente o mostrador físico.

**RF-WAR-4** Verificación automática: ¿En período? ¿S/N corresponde a venta propia? ¿Garantías previas por esta unidad?

**RF-WAR-5** Flujo: Diagnóstico → Reparar / Reponer stock / Reembolso. Cada decisión publica Integration Event.

**RF-WAR-6** SLA configurable por categoría. Alerta cuando un caso está próximo a vencer.

**RF-WAR-7** Reclamación automática al fabricante cuando la falla es de fábrica. `WarrantyClaimEvent` → proveedor.

**RF-WAR-8** Reporte de tasa de falla por marca/producto para negociación con fabricantes.

#### Módulo BI — Inteligencia de Negocio

**RF-BI-1** Dashboards con `SfChart` y `SfGrid`. Actualización en tiempo real vía SSE.

**RF-BI-2** Dashboard CEO: ventas del día vs. período anterior, pedidos por estado, top 10 productos/clientes, inventario crítico, saldo de caja.

**RF-BI-3** Dashboard Ventas: pipeline por etapa, tasa de conversión por vendedor/canal, forecast 30 días.

**RF-BI-4** Dashboard Inventario: stock por bodega/categoría, días de cobertura, próximas importaciones.

**RF-BI-5** Dashboard Logística: pedidos en tránsito, tasa devoluciones por transportadora, recaudos pendientes.

**RF-BI-6** Dashboard Financiero: P&L simplificado, flujo de caja 30 días, cartera vencida.

**RF-BI-7** Dashboard Garantías: casos abiertos, SLA próximo a vencer, tasa de falla por marca, costo acumulado.

**RF-BI-8** BI predictivo: predicción de agotamiento de stock, scoring de abandono de clientes, alertas de anomalías en ventas.

#### Módulo Automation — Workflows

**RF-AUTO-1** Motor visual sin código: Trigger → Condición → Acción.

**RF-AUTO-2** Triggers: nuevo lead, cambio de etapa pipeline, pedido pagado, stock bajo mínimo, garantía abierta, N días sin actividad del cliente, mensaje WhatsApp recibido.

**RF-AUTO-3** Acciones: enviar WhatsApp (plantilla HSM), enviar email, crear tarea, cambiar estado, asignar lead, generar solicitud de compra, publicar Integration Event en RabbitMQ.

**RF-AUTO-4** Implementación: Hangfire para jobs recurrentes, MassTransit Sagas para flujos con estado (ej. ciclo completo de importación).

#### Módulo AI Services — Capa de Inteligencia Artificial

**RF-AI-1** AI Copywriter: generación de títulos y descripciones SEO vía Claude API (Anthropic). Descripción 1600+ palabras, meta título ≤60 chars, meta descripción 135-139 chars.

**RF-AI-2** Sales Bot: chatbot WhatsApp que califica leads (BANT), responde dudas técnicas, ejecuta flujo de venta.

**RF-AI-3** Lead Scoring predictivo: aprende qué características correlacionan con conversión.

**RF-AI-4** Resúmenes de conversación: IA resume hilos largos de WhatsApp para el equipo comercial.

**RF-AI-5** Conciliación bancaria asistida: movimientos sin coincidencia automática presentados al contador con sugerencia IA.

---

### FASE 4: ESCALA Y DIFERENCIACIÓN
**Semanas 25-36 | Criterio de éxito:** Sistema percibido como ventaja competitiva.**

#### Módulo Dropshipping

**RF-DROP-1** Identificación automática al confirmar pedido: ¿stock propio o dropshipping?

**RF-DROP-2** Notificación al proveedor (email + API). Portal del proveedor: órdenes pendientes, ingreso de guía, confirmación de despacho.

**RF-DROP-3** Liquidación periódica automática: cobrado al cliente - comisión = monto al proveedor.

**RF-DROP-4** Evaluación del proveedor: tasa de cumplimiento, tasa de garantías, tiempo de entrega.

#### Portal B2B Distribuidores

**RF-B2B-1** Acceso por invitación con aprobación manual.

**RF-B2B-2** Catálogo con precios diferenciados. Conversión automática de Orden de Compra del cliente a pedido en el sistema.

**RF-B2B-3** Límites de crédito con alertas. Historial completo accesible desde el portal.

#### WMS — Gestión de Bodegas

**RF-WMS-1** Jerarquía de ubicaciones recursiva: Bodega → Pasillo → Estante → Nivel → Posición. Tabla con `ParentId` para cualquier profundidad.

**RF-WMS-2** Etiquetas QR por ubicación. Picking optimizado con ruta sugerida.

**RF-WMS-3** Preparar arquitectura para RFID sin implementar en esta fase.

#### Módulo Integrations — API Propia

**RF-INT-1** API REST: OpenAPI 3.0 (Scalar), OAuth 2.0, rate limiting, sandbox, webhooks configurables por tenant.

**RF-INT-2** Integraciones de Tecnoimportaciones:

| Sistema | Propósito | Fase |
|---|---|---|
| WooCommerce + Electro | Sync pedidos/inventario/productos bidireccional | 1 |
| Wompi / PayU | Confirmación pagos | 1 |
| Coordinadora, Servientrega, TCC, Interrapidísimo, Domina, Envía | Guías y tracking | 1 |
| DIAN vía Alegra | Facturación electrónica + nómina | 1 |
| WhatsApp Business API | Mensajería omnicanal | 2 |
| Banco República Colombia | TRM diaria automática | 2 |
| Bancolombia / Davivienda | Extractos para conciliación | 2 |
| Google Calendar / Outlook | Sync reuniones | 2 |
| Meta Pixel / Google Analytics | Eventos de conversión | 3 |
| Claude API (Anthropic) | AI Copywriter, bot, resúmenes | 3 |

---

## 8. REGLAS DE RENDIMIENTO Y SEGURIDAD

```
Performance:
- Queries de inventario: < 200ms
- AsNoTracking() en TODAS las queries de lectura (Specification<T> lo hace por defecto; en queries manuales, explícito)
- Caché Redis para catálogos: TTL configurable, latencia < 50ms
- Paginación server-side en todos los listados. NUNCA traer todos los registros.
- Índices PostgreSQL obligatorios en: TenantId, SKU, SerialNumber, CustomerId, OrderId, CreatedAt

Multitenancy:
- Finbuckle.MultiTenant maneja el aislamiento — no implementar TenantId manualmente
- Los Global Query Filters de EF Core filtran por TenantId automáticamente en el DbContext base
- En queries manuales fuera del DbContext: filtrar por TenantId explícitamente
- Ningún endpoint puede acceder a datos de otro Tenant aunque el usuario esté autenticado

Seguridad:
- Ningún endpoint sin .RequirePermission() — sin excepciones
- Nunca exponer IDs de base de datos en URLs → usar GUIDs generados en el dominio
- Documentos aprobados/facturados: inmutables. Lógica de bloqueo en las entidades.
- Los logs de Serilog no deben incluir PII en texto plano
```

---

## 9. REGLAS PROHIBIDAS — NUNCA VIOLAR

```
🚫 PROHIBIDO: Modificar src/BuildingBlocks/ sin aprobación explícita de Juan
   Razón: blast radius total — afecta todos los módulos

🚫 PROHIBIDO: Referenciar el runtime de otro módulo (solo sus .Contracts)
   Razón: viola los module boundaries de FSH
   Correcto: Maka.Modules.Orders → Maka.Modules.Inventory.Contracts
   Incorrecto: Maka.Modules.Orders → Maka.Modules.Inventory

🚫 PROHIBIDO: MediatR clásico (reflexión)
   Razón: FSH usa Mediator 3.0.1 source-generated. Son incompatibles.
   Usar: ICommand<T>, IQuery<T>, ICommandHandler<T,R>, IQueryHandler<T,R> del framework

🚫 PROHIBIDO: AutoMapper o mapeo automático por reflexión
   Solución: métodos de extensión estáticos en [Modulo]/Extensions/ (ej. ProductExtensions.ToDto())

🚫 PROHIBIDO: Patrón Repository genérico (IRepository<T>)
   Solución: DbContext directo en los Handlers + Specification<T> del framework para queries complejas

🚫 PROHIBIDO: MudBlazor en cualquier forma
   Solución: Syncfusion Blazor exclusivamente

🚫 PROHIBIDO: MassTransit v9 (licencia comercial)
   Solución: v8.5.7 Apache 2.0. Si v9 se requiere: escalar a Juan para decisión.

🚫 PROHIBIDO: usar SignalR donde SSE es suficiente (y viceversa)
   Regla: flujo unidireccional → SSE. Flujo bidireccional/colaborativo → SignalR.
   Ver `.agents/rules/realtime.md` para la tabla de decisión completa y los patrones de implementación.

🚫 PROHIBIDO: Swashbuckle/Swagger
   Razón: FSH usa Scalar para documentación API
   Solución: configurar con OpenAPI + Scalar como viene en FSH

🚫 PROHIBIDO: Lógica de negocio en Handlers
   Regla: Handlers orquestan. La lógica va en las entidades (Domain). La validación va en Validators.

🚫 PROHIBIDO: Adivinar parámetros de Meta API, endpoints DIAN o componentes Syncfusion sin documentación
   Solución: parar y pedir documentación oficial al usuario

🚫 PROHIBIDO: Credenciales o API keys en código o archivos commiteados
   Solución: appsettings.Development.json (en .gitignore) o .NET Secret Manager

🚫 PROHIBIDO: Crear o modificar cualquier componente UI sin usar los wrappers Maka* de
   Syncfusion para componentes complejos.

   Mapa de componentes obligatorio:
   - Tablas / listados de datos    → MakaGrid
   - Gráficas / charts             → MakaChart
   - Tableros Kanban               → MakaKanban
   - Tablas pivote / analytics     → MakaPivot
   - Calendarios / agenda          → MakaScheduler
   - Inputs de texto               → SfTextBox
   - Selects / dropdowns           → SfDropDownList
   - Date pickers                  → SfDatePicker
   - Botones, badges, cards,
     avatars, skeleton             → componentes Tailwind nativos existentes
                                      (NO reemplazar, ya están bien construidos)

🚫 PROHIBIDO: Agregar texto visible al usuario sin usar t() de i18next Y sin crear
   las traducciones en TODOS los idiomas configurados.

   PROCESO OBLIGATORIO para cualquier texto nuevo:

   Paso 1 — Identificar el namespace correcto:
   common, catalog, inventory, orders, crm, settings,
   billing, logistics, warranties, imports, whatsapp, hr

   Paso 2 — Agregar la key en español primero:
   public/locales/es/[namespace].json

   Paso 3 — Agregar la traducción en inglés:
   public/locales/en/[namespace].json

   Paso 4 — Si hay más idiomas en public/locales/:
   Agregar en TODOS los idiomas existentes sin excepción.

   Paso 5 — Recién entonces usar t('namespace:key') en el componente.

   Reglas adicionales:
   - NUNCA dejar una key sin traducción en algún idioma configurado
   - NUNCA usar strings de texto directos en JSX/TSX aunque sea "solo temporal"
   - NUNCA usar el namespace 'common' para strings específicos de un módulo
   - Si no se conoce la traducción exacta en inglés, usar una aproximación razonable
     y agregar comentario: // TODO: review translation

🚫 PROHIBIDO: Guardar configuración, preferencias o datos de usuario/apariencia/
   localización en localStorage, sessionStorage o variables globales como fuente
   de verdad.

   REGLA — Todo debe persistir por Tenant:
   Todo lo que sea configurable por una empresa (tenant) DEBE persistir en la
   base de datos bajo el TenantId correspondiente. Esto incluye sin excepción:
   - Configuración de apariencia (tema, acento, tipografía)
   - Configuración de localización (timezone, moneda, idioma, formato de
     fecha/hora/números)
   - Cualquier preferencia futura de configuración del tenant

   PATRÓN CORRECTO:
   1. Backend: tabla con TenantId + campos de configuración
   2. Frontend: cargar desde API al iniciar sesión
   3. Frontend: guardar via API (PUT/PATCH) cuando el usuario cambia algo
   4. Frontend: usar localStorage SOLO como caché temporal para evitar flicker
      en el boot — siempre sincronizar con la BD como fuente de verdad

🚫 PROHIBIDO: Crear componentes que no respeten el sistema de temas y apariencia
   del dashboard.

   1. NUNCA usar colores hardcodeados (hex, rgb, hsl). Siempre usar tokens CSS:

      Texto:       var(--color-text-primary/secondary/tertiary/disabled)
      Superficies: var(--color-bg-primary/secondary/tertiary/elevated)
      Bordes:      var(--color-border-primary/secondary/tertiary)
      Acento:      var(--color-accent) / var(--color-accent-subtle) / var(--color-accent-muted)
      Semánticos:  var(--color-success/warning/danger/info) + variantes -subtle
      Charts:      var(--color-chart-1..5) / var(--color-saffron)

   2. NUNCA usar clases Tailwind de color fijas: bg-white, text-black, bg-gray-100,
      text-gray-600, border-gray-200, etc. Usar clases semánticas del proyecto o
      tokens CSS directos.

   3. Syncfusion usa canvas — no lee CSS vars automáticamente. SIEMPRE resolver
      colores en mount con:
        getComputedStyle(document.documentElement).getPropertyValue('--color-chart-1').trim()
      Patrón de referencia: MakaChart.tsx — seguir ese ejemplo exactamente.

   4. Todo componente nuevo debe verse correctamente en:
      - Tema Light y Dark
      - Con cualquiera de los 6 acentos: rose, indigo, violet, sky, emerald, amber + custom
      - Con cualquiera de las tipografías disponibles

   5. Antes de hacer commit de un componente nuevo, verificar visualmente en
      Light + Dark mode mínimo.

   6. Si un componente Syncfusion no respeta dark mode automáticamente, agregar
      CSS override usando:
        [data-theme="dark"] .e-grid { ... }
        [data-theme="dark"] .e-kanban { ... }
        [data-theme="dark"] .e-schedule { ... }
```

---

## 9b. REGLA — Reinicio del servidor después de cambios backend

Cuando se hagan cambios al backend que requieran reinicio del servidor, SIEMPRE
indicar explícitamente al usuario:

```
"Para aplicar estos cambios necesitas reiniciar la API.
 Ejecuta en PowerShell:
 Get-Process dotnet | Stop-Process -Force
 dotnet run --project src/Host/FSH.Starter.Api"
```

Nunca asumir que el servidor se reinició automáticamente.
Nunca hacer commit del backend sin antes verificar que el endpoint funciona
con la API reiniciada.

---

## 11. WORKFLOW DE DESARROLLO

```
Por cada nueva feature:

1. PLAN MODE (Shift+Tab):
   - Proponer entidad de dominio y propiedades
   - Proponer Command/Query en .Contracts + Validator
   - Proponer Handler + tablas/relaciones DB afectadas
   - Identificar Integration Events que se publican y quién los consume
   ⏸ Esperar confirmación de Juan antes de escribir código

2. IMPLEMENTAR en orden:
   a) Entidad en Domain/ con lógica de negocio y domain events
   b) Command/Query en .Contracts/v1/{Area}/{Feature}/
   c) Validator en Features/v1/{Area}/{Feature}/
   d) Configuración EF Core en Data/
   e) Migración: dotnet ef migrations add NombreDescriptivo
   f) Handler en Features/v1/{Area}/{Feature}/
   g) Endpoint en Features/v1/{Area}/{Feature}/
   h) Integration Event (si aplica) + Consumer en módulo receptor
   i) Componente Blazor/Syncfusion
   j) Test en Tests/{Modulo}.Tests/

3. Commit granular por cada paso

4. Al terminar módulo: dotnet test src/FSH.Starter.slnx

5. Al terminar tarea grande: /clear en Claude Code
```

**Convención de commits:**
```
feat(inventory): add SerialNumber entity with warranty tracking
feat(crm): implement SfKanban pipeline view
feat(billing): integrate Alegra API for DIAN electronic invoicing
fix(orders): correct stock reservation TTL on payment timeout
refactor(catalog): extract Product mapping to extension methods
chore(db): add migration Inventory_AddSerialNumbers
test(inventory): add integration tests for stock transfer flow
```

---

## 12. SEÑALES DE ALERTA — PARAR Y PREGUNTAR

```
⚠️  Modificar src/BuildingBlocks/ o src/Modules/Identity/
⚠️  Un módulo quiere referenciar el runtime (no los Contracts) de otro módulo
⚠️  Una migración elimina o renombra una columna con datos existentes
⚠️  El Handler crece más de ~50 líneas → lógica que debe ir al dominio
⚠️  Aparece alguna referencia a MudBlazor o SignalR o Swashbuckle
⚠️  MassTransit sugiere actualizar a v9
⚠️  Se va a enviar comunicación (WhatsApp, email, webhook) en producción
⚠️  Ambigüedad en una regla de negocio colombiana (impuesto, retención, garantía)
⚠️  Se necesita la Syncfusion license key y no está configurada
```

---

## 13. ACCESOS PENDIENTES — COMPLETAR ANTES DE FASE 1

```
Syncfusion License Key:         PENDIENTE — syncfusion.com/account
Meta WhatsApp App ID:           PENDIENTE
Meta Phone Number ID:           PENDIENTE
Meta Webhook Verify Token:      PENDIENTE
Meta Access Token:              PENDIENTE
Alegra API Key (DIAN PTA):      PENDIENTE
Banco República Colombia API:   PENDIENTE (TRM automática)
Wompi API Key:                  PENDIENTE
PayU Merchant ID + API Key:     PENDIENTE
Coordinadora API Key:           PENDIENTE
Servientrega API Key:           PENDIENTE
Interrapidísimo API Key:        PENDIENTE
TCC API Key:                    PENDIENTE
Claude API Key (Anthropic):     PENDIENTE (AI Copywriter + bot)
```

---

## 14. FUNCIONALIDADES EXCLUIDAS — NO DESARROLLAR

```
❌ Manufactura / MRP (distribuidora, no fabricante)
❌ Constructor de sitio web (la tienda sigue en WooCommerce)
❌ Marketplace multi-vendor
❌ SignalR para casos de uso unidireccionales (usar SSE en su lugar — ver `.agents/rules/realtime.md`)
❌ Swashbuckle/Swagger (FSH usa Scalar)
❌ MassTransit v9 (comercial)
❌ MudBlazor (purgado)
❌ Realidad aumentada / 3D en productos
❌ Gestión de anuncios pagados (Google Ads, Meta Ads)
❌ iMessage, Telegram, TikTok Messages
❌ Call center con IVR complejo
❌ Contabilidad multi-país simultánea
❌ RRHH avanzado (evaluaciones 360°, ATS)
❌ Multi-empresa con consolidación automática (una sola razón social)
❌ Base de datos B2B global tipo Apollo.io
```

---

---

## 15. PROTOCOLO DE CALIDAD — OBLIGATORIO ANTES DE CADA COMMIT

Antes de hacer cualquier commit, Claude debe actuar como su propio QA ejecutando este ciclo completo:

### Para cambios de BACKEND:
```
1. dotnet build src/FSH.Starter.slnx → 0 errores
2. Verificar que el endpoint existe en Scalar/OpenAPI:
   abrir http://localhost:5000/scalar y buscar la ruta
3. Hacer llamada de prueba al endpoint con curl o .http:
   curl -X GET/POST/PUT http://localhost:5000/api/v1/...
4. Verificar respuesta HTTP correcta (200, 201, etc.)
5. Si el endpoint retorna 404: el endpoint no está
   registrado — revisar MapEndpoints() en el módulo
```

### Para cambios de FRONTEND:
```
1. npm run build → 0 errores TypeScript
2. npm run dev → abrir el browser
3. Navegar a CADA página/componente modificado
4. Ejecutar el flujo completo: crear → editar →
   filtrar → eliminar lo creado
5. Verificar en Light mode Y Dark mode
6. Abrir DevTools → Console → 0 errores rojos
7. Abrir DevTools → Network → verificar que las
   llamadas HTTP retornan 200 (no 404, no 500)
8. Si hay errores de red: CORREGIR antes del commit
```

### Para cambios FULL STACK:
Ejecutar ambas listas en orden: backend primero, luego frontend.

### REGLA DE ORO:
```
NUNCA hacer commit si hay errores en Console o Network tab
del browser. Un commit con errores visibles es un commit inválido.
```

### QA Multitenancy obligatorio:
```
Si el sistema tiene multitenancy, las pruebas DEBEN hacerse con al
menos 2 tenants distintos para confirmar aislamiento de datos:

1. Hacer el cambio/acción con el tenant A (ej. root)
2. Verificar que el tenant B (ej. acme) NO ve los datos del tenant A
3. Hacer el cambio/acción con el tenant B
4. Verificar que el tenant A NO ve los datos del tenant B
5. Específicamente para configuración (apariencia, localización):
   - Cambiar timezone en root → verificar que acme mantiene su timezone
   - Cambiar tema en acme → verificar que root mantiene su tema
```

---

*Fuente de verdad del proyecto. Si hay conflicto con cualquier otra instrucción, este archivo tiene prioridad.*
*Versión: 3.1 | Proyecto: Maka Omni-Commerce Ecosystem | Mayo 2026*
