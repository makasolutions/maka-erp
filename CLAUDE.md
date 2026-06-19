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
7. ¿El modelo activo es el óptimo para esta tarea?
   → Consultar `.agents/rules/model-routing.md`
   → Si no es el óptimo: mostrar el banner de cambio ANTES de proceder

Si cualquier respuesta es dudosa: **PARAR y preguntar antes de continuar.**

---

## SELECCIÓN DE MODELO

Ver reglas completas en `.agents/rules/model-routing.md`

Resumen:

| Modelo | Cuándo usarlo |
|---|---|
| **Sonnet** | Implementación de feature aprobada, bugs localizados, revisión y lectura de docs |
| **Opus** | Arquitectura nueva, bugs complejos sin causa clara, features multi-módulo (3+), revisión de seguridad crítica |
| **Haiku** | Confirmaciones rápidas (sí/no), traducciones, renombrados, tareas mecánicas repetitivas |

Cuando el modelo actual no sea el óptimo: mostrar el banner de cambio (definido en el archivo de reglas) y esperar confirmación antes de proceder.

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
| **ERP a reemplazar** | Effi / Efficommerce | Siigo | Mercatelly | Kommo | 
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

## 9c. REGLA — Debugging de errores/500 vía módulo de Auditoría (NO try/catch temporal)

El **GlobalExceptionHandler** oculta el detalle en la respuesta (solo título genérico) y la
consola del API solo loguea el resumen. **PERO** el módulo de **Auditoría captura toda
excepción real** (`AuditHttpMiddleware`): tipo, mensaje, **stack completo**, ruta, `TraceId`,
`CorrelationId`, tenant y usuario (`EventType=4`, `Severity=Error`).

> 🚫 PROHIBIDO instrumentar el código con `try/catch` temporales, logs ad-hoc o cambiar el
> `GlobalExceptionHandler` para diagnosticar un 500. Eso ensucia el código de producción y hay
> que revertirlo. **La auditoría ya es la fuente de verdad de las excepciones.**

**Flujo obligatorio para diagnosticar un error/500:**
1. Tomar el `traceId` de la respuesta del error (también visible en el toast del front).
2. Leer la excepción completa por cualquiera de estas vías:
   - **BD (lo más rápido):**
     ```
     docker exec maka_postgres psql -U maka_user -d maka_erp_dev -c \
       "SELECT jsonb_pretty(\"PayloadJson\") FROM audit.\"AuditRecords\" \
        WHERE \"TraceId\"='<traceId>' AND \"EventType\"=4;"
     ```
   - **API:** `GET /api/v1/audits/by-trace/{traceId}` → tomar el `id` del evento Excepción →
     `GET /api/v1/audits/{id}` (detalle con `payloadJson`: `exceptionType`, `message`, `stackTop`).
   - **UI:** Sistema → Registro de auditoría → filtro **Excepción** (o **Buscar** por traceId) →
     clic en la fila → drawer con el stack completo.
3. Corregir la **causa raíz** y re-probar. Sin instrumentación temporal.

Severidad: cancelaciones → Information, no-autorizado → Warning, **todo lo demás → Error**
(`MinExceptionSeverity=Error`, así que las excepciones reales siempre se auditan). La auditoría de
excepción se encola con `TryWrite` no bloqueante (independiente de la cancelación del request),
por lo que se registra aunque el cliente se desconecte.

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

## 16. ESTADO DE AVANCE — Marketplace / Dropshipping global

> Bitácora de lo construido sobre el plan de red de proveedores. Cada fase está
> commiteada y verificada (build 0 err + Architecture.Tests 49/49).

### Terceros / HR (previo a marketplace)
- **Módulo `Parties`** (order 550): master de Terceros (cliente/proveedor combinables;
  empleado exclusivo). Wizard de 5 pasos (Identificación+Direcciones+Canales / Contactos /
  CRM / Financiera / Tributaria). Direcciones Ciudad→Departamento; contactos con correo+celular.
- **Módulo `Lookups`** (order 520): Tablas Básicas (picklists `IGlobalEntity`, TenantId null).
- **Módulo `Hr`** (order 560): `Employee` 1:1 con Party; pantalla Empleados (wizard 3 pasos).

### Arquitectura marketplace — **tenant `global` dedicado**
- `MultitenancyConstants.Global` (id `global`). Provisionado por el DbMigrator junto a `root`.
  Contiene el catálogo compartido (taxonomía, marcas, industrias, productos/proveedores globales).
- **`IGlobalCatalogReader`** (Catalog): lee el tenant `global` desde cualquier tenant abriendo
  un scope hijo + `IMultiTenantContextSetter`. NO se modifica `BaseDbContext`.
- Picklists universales se quedan como `IGlobalEntity` (no migran al tenant global).

### Fases entregadas
- **Fase A** ✅ — tenant `global` provisionado y aislado.
- **Fase B** ✅ — integridad de identificación: índice único **por tenant** `(TenantId, IdType,
  IdNumber)`; `IdentificationValidator` (DV NIT + formato); `IIdentityVerificationProvider` con
  `RuesIdentityVerificationProvider` (flag `IdentityVerification:Provider=Rues|None`, tolerante);
  endpoint `POST /parties/verify-identification` + botón "Verificar" en el wizard.
- **Fase C** ✅ — taxonomía Google es-ES (5.595 categorías, embebida `WithCulture=false`) +
  40 marcas canónicas + 16 **Industrias** (mapeo a raíces Google) sembradas en `global`.
  `Category` ganó `GoogleCategoryId/RootGoogleCategoryId/FullPath`. Endpoints `/catalog/global`:
  industries, tenant-industries (GET/PUT), **global-categories filtradas por la industria del
  tenant** (sin industria → taxonomía completa). UI: Configuración→Catálogo→"Industrias del tenant".
- **Fase D** ✅ — relación comercial:
  - D-1: **`PartyPriceList`** (asignación de listas de precios a clientes/proveedores) +
    pestaña "Listas de precios" en la ficha del tercero.
  - D-2: mapeo proveedor↔catálogo — `SupplierCategory` (+ `SupplierBrand`/`SupplierProduct`
    existentes); endpoints `/catalog/suppliers/{id}/(brands|categories)` + `GET /global-brands`;
    pestaña "Catálogo proveedor" (marcas + categorías globales filtradas por industria).
  - D-3: **`Party.IsGlobalSupplier`** + endpoint `PUT /parties/{id}/global-supplier` + toggle
    "Proveedor global" en la pestaña del proveedor.
- **Fase E** ✅ — **Convenios** (decisión: extensión de **Catalog**, no módulo `Suppliers`).
  - Entidades `Agreement` + `AgreementRule` (tenant-aisladas; **inmutables en `Terminado`**);
    tarifa acordada/sugerida = FK a `PriceList`; responsables (despacho/guía/liquidación);
    políticas (entregas fallidas/devoluciones/garantías); reglas configurables.
  - Enums con `JsonStringEnumConverter`: `AgreementType/Status/Responsible/RuleType` +
    `RuleEvaluationResult`. Migración `Catalog_Agreements`. Permiso `Catalog.Agreements`.
  - Endpoints `api/v1/catalog/agreements` (list/detail/create/update/`/status`/delete/`/evaluate`).
  - **Motor de evaluación** (`/evaluate?distributorId=`): lee el Party del distribuidor vía
    `Parties.Contracts` + `IMediator` (`GetPartyByIdQuery`; se agregó `CreatedAtUtc` al DTO).
    Resuelve `AntiguedadMinimaMeses`, `VendeAEmpresa/Natural` (Kind), `DocumentoExigido`; las
    métricas de ventas/calificación/SLA/cumplimiento → **`Pendiente`** hasta Fase F.
    `Eligible` = ninguna regla **obligatoria** en `NoCumple` (las `Pendiente` no bloquean).
  - Front: `pages/crm/convenios.tsx` (MakaGrid + editor con reglas repetibles + panel de
    evaluación + transiciones de estado), menú **Comercial → Convenios**, i18n ES/EN.
  - Gotchas resueltos: fechas `timestamptz` → `AsUtc()` (Kind=Unspecified rompe Npgsql);
    nombre del proveedor resuelto **en el backend** para la lista (evita fragilidad de timing
    en el front); proveedor de solo-lectura al editar (el `PartyPicker` deshabilitado no
    pinta su label). El costo/PV sugerido/ganancia en la tarjeta de producto se difiere a Fase G.

- **Fase F** ✅ — **Evaluación de proveedores** (scorecard ponderado), también en **Catalog**.
  - Entidades `ScorecardKpi` (catálogo de KPIs con **peso editable** por tenant),
    `SupplierScorecard` (+ `ScorecardCriterion` snapshot de KPI) — **inmutable en `Cerrado`**.
    `Recompute()`: `WeightedScore = Σ(score·peso)/Σ(peso)` (1–5) → `Grade` A≥4.5·B≥3.5·C≥2.5·D≥1.5·F.
  - Enums `ScorecardStatus/Grade` (`JsonStringEnumConverter`). Migración `Catalog_SupplierEvaluation`.
    Permiso `Catalog.Scorecards`. KPIs por defecto vía `POST .../scorecard-kpis/seed-defaults`
    (Calidad 35·Entrega 25·Precio 20·Servicio 10·Cumplimiento 10).
  - Endpoints `api/v1/catalog/{scorecard-kpis,supplier-scorecards}` (+ `/ranking`, `/trend/{id}`,
    `/{id}/close`). Reportes: ranking (último score por proveedor) y tendencia por período.
  - **Enganche E↔F**: la regla `CalificacionMinima` del convenio ahora lee el **último scorecard
    del distribuidor** (`Cumple/NoCumple` con el score) en vez de `Pendiente`.
  - Front: `pages/crm/evaluacion-proveedores.tsx` (pestañas Scorecards/Reportes/KPIs; editor con
    score+letra en vivo; `MakaChart` ranking/tendencia), menú **Comercial → Evaluación de
    proveedores**, i18n ES/EN.
  - Gotcha: el namespace de features se llamó **`SupplierEvaluation`** (no `Scorecards`) porque
    NetArchTest detecta el substring `core` dentro de "S**core**cards" en la regla de pureza `.Core.`.

- **Búsqueda inteligente + adopción** ✅ — al crear **categoría/marca** se busca en el catálogo
  global con tolerancia a typos/acentos/sinónimos y se sugiere adoptar (editando nombre/slug/etc.).
  - Postgres `pg_trgm` + `unaccent` en `CatalogDbContext`; migración `Catalog_FuzzySearch`.
    Entidad **`CatalogAlias`** (tenant `global`: EntityType Category|Brand + TargetId + Alias) con
    índice GIN trigram; semilla de 25 alias (`cannon`→Canon, `camara`→Cámaras…, `celular`→Teléfonos).
  - Búsqueda por **SQL crudo** dentro de `global.RunAsync`: `similarity(unaccent(lower(Name)), …)`
    + match por alias; filtro por industria (roots); `TenantId='global'` explícito; flag
    `AlreadyAdopted` (categorías por GoogleCategoryId, marcas por Slug). Endpoints
    `/catalog/global/{search-categories,search-brands,adopt-category,aliases}`.
  - `AdoptGlobalCategoryCommand`: adopta con nombre/slug **editables**, recreando ancestros y
    deduplicando. Marca: la adopción precarga el form y usa `createBrand`.
  - Front: `GlobalSuggestionField` (input con sugerencias debounced + chip "ya en tu catálogo")
    en los creadores de Categorías y Marcas; gestor de alias en **Configuración → Catálogo**.
  - **Productos diferidos a Fase G** (no hay catálogo global de productos); el patrón queda listo.
  - Gotcha: el seeder global corre con `DbMigrator -- seed` (no en `apply`); los SaveChanges de
    alias en el scope global escriben con `TenantId='global'`.

### Pendiente (siguientes fases)
- **Fase G** — Bodega/inventario global (stock público compartido) + tarjeta de producto
  dropshipping (Costo desde la lista atada al convenio · PV sugerido · Ganancia) + búsqueda
  inteligente de **productos** globales en el creador de productos (reusar `GlobalSuggestionField`).

### Convenciones nuevas confirmadas
- Datos de marketplace → tenant `global`; lectura cruzada solo vía `IGlobalCatalogReader`.
- Truncate de categorías **solo** en `global`, nunca en tenants cliente.
- Recursos embebidos con `.xx-YY.` en el nombre → `WithCulture="false"` (evita satélites de cultura).
- Índices únicos que dependan del shadow `TenantId` se definen en el DbContext **tras**
  `base.OnModelCreating`.
- Password root dev: `123Pa$$word!`; token endpoint requiere header `tenant: root` + `X-FSH-App: admin`.

---

## 17. ESTÁNDAR DE VALIDACIÓN DE FORMULARIOS — OBLIGATORIO

> Toda entrada de datos del usuario DEBE validarse **por tipo de dato**, no campo por campo
> de forma ad-hoc. Cada campo se "tipa" y hereda las validaciones mínimas: **requerido ·
> tipo · longitud mínima · longitud máxima · regex/formato · reglas de negocio**.

**Arquitectura (doble defensa, una fuente de verdad):**
- **Backend = fuente de verdad.** FluentValidation. Helpers reutilizables en
  `src/Modules/Parties/.../Domain/FormValidationRules.cs` (`IsValidPersonName`, `IsValidUrl`,
  `IsValidPhone`, `IsValidColombianAddress`, `IsValidLatitude/Longitude`, `BirthDateError`,
  `IsValidChannelValue`). Mensajes SIEMPRE en español.
- **Frontend = espejo 1:1** en `clients/dashboard/src/lib/validation/forms.ts` (mismas regex y
  rangos). `validateParty` / `validateIdentity` devuelven `{ campo → mensaje }` vía `t('common:validation.*')`.
- **UI:** el `Field` (`components/list/field.tsx`) acepta `error?` → tinta el borde del control y
  muestra el mensaje. La página corre el validador **al hacer submit** (`showErrors`), pinta
  errores inline + `FormErrorSummary` + puntos rojos en las pestañas. El botón Guardar NO se
  deshabilita: el submit muestra los errores. **Nunca** dejar pasar texto directo sin `t()`.

**Reglas mínimas por tipo de dato (tabla canónica):**

| Tipo | Req. | Min | Max | Formato / regla |
|---|---|---|---|---|
| NombrePersona (nombres/apellidos) | sí si Natural | 2 | **50** | letras Unicode + ` . ' -`; sin dígitos; sin repetir ≥8 |
| RazónSocial | sí si Jurídica | 3 | **150** | imprimibles |
| Email | según campo | — | 254 | `^[^\s@]+@[^\s@]+\.[^\s@]+$` |
| Url / Website | no | — | 2048 | `http(s)://` válido; antepone `https://` si falta esquema |
| Teléfono/Celular | según campo | — | — | **CO `^3\d{9}$`** o **E.164 `^\+\d{7,15}$`** (internacional) |
| NIT | sí | 5 | 15 | dígitos; DV calculado (DIAN), **no editable** |
| Identificación **por tipo** | sí | — | — | El formato depende del `IdType` seleccionado (tabla abajo). **NUNCA** permitir letras en docs numéricos; máscara solo-dígitos al teclear. |
| Latitud | no | — | — | **−90 … 90** (bloqueo duro) |
| Longitud | no | — | — | **−180 … 180** (bloqueo duro); lat/lng van juntas |
| Dirección (DIAN) | **sí** | — | — | `<vía> <n> # <n>-<n>`. Ej. `CL 100 # 13-21` |
| Fecha de nacimiento | no | — | — | **estrictamente < hoy** (nunca hoy ni futura) **y ≥ hoy−120 años**. Para **personas de contacto** además **edad ≥ 15 años** (ver nota legal). |
| Fecha de creación/registro/emisión | — | — | — | **NUNCA futura** (≤ hoy/ahora). Aplica a toda fecha que represente "cuándo ocurrió/se creó algo". |
| Rango de fechas (desde/hasta) | — | — | — | **`inicio ≤ fin`** siempre; `fin` nula = vigente. La inicial nunca puede ser mayor que la final. |
| Valor de canal | sí | — | 256 | formato según tipo (Email→@, Web→url, Tel→phone) |

**Identificación colombiana — regex por tipo (front + back):**

| IdType | Regex | Notas |
|---|---|---|
| CC (Cédula de ciudadanía) | `^\d{6,10}$` | solo dígitos, 6–10 |
| TI (Tarjeta de identidad) | `^\d{8,11}$` | solo dígitos |
| NUIP | `^\d{8,11}$` | solo dígitos |
| CE (Cédula de extranjería) | `^[A-Za-z0-9]{6,15}$` | alfanumérico |
| NIT | `^\d{9,10}$` + DV | DV calculado, no editable |
| Pasaporte | `^[A-Za-z0-9]{6,15}$` | alfanumérico |

> **Edad mínima de personas de contacto (regla de negocio Maka):** un contacto comercial de una
> empresa debe tener **≥ 15 años**. Justificación: en Colombia la edad mínima para trabajar es 15
> (Código de la Infancia y la Adolescencia, Ley 1098/2006, art. 35, con permiso) y la capacidad
> laboral plena es a los 18; no es lógico registrar como contacto comercial a alguien menor. Se
> adopta **15** como piso pragmático; endurecer a 18 si el negocio lo requiere. **No** se conoce
> norma que prohíba el dato en sí, pero esta validación previene errores de digitación.

**Reglas de negocio de Terceros (obligatorias):**
- Un tercero de tipo **empresa (Jurídica)** DEBE tener **al menos un contacto** (persona de contacto).
- Toda dirección y contacto se editan con sus **componentes centralizados reutilizables**
  (`AddressEditor`, `ContactEditor`, `ChannelEditor`) — ver §18 mandato de reuso.

**Nomenclatura DIAN (vías aceptadas, abreviatura canónica):**
`CL` Calle · `KR` Carrera · `AV` Avenida · `AC` Av. Calle · `AK` Av. Carrera · `DG` Diagonal ·
`TV` Transversal · `CQ` Circular · `CV` Circunvalar · `AU` Autopista · `KM` Kilómetro · `MZ`
Manzana · `VRD` Vereda. (Validación pragmática por regex; constructor estructurado = futuro.)

**Máscaras:** documentos numéricos (NIT/CC/TI/NUIP) filtran a solo-dígitos al teclear;
lat/lng usan `type=number` con `min/max`. Email y web → `Input` + regex (no máscara fija, porque
son de longitud variable). Para nuevos campos de formato fijo, preferir filtro de entrada antes
que un componente Syncfusion que rompa los tokens de tema.

> 🚫 PROHIBIDO agregar un campo de formulario sin asignarle su tipo de validación de esta tabla
> (front + back). Si un tipo no existe aún, extender `FormValidationRules.cs` y `forms.ts` a la par.

---

## 18. ESTÁNDAR DE AUDITORÍA Y QA EMPRESARIAL — OBLIGATORIO

> **Premisa central:** asumir que el usuario puede **digitar cualquier cosa en cualquier campo**
> (entrada hostil/no confiable). NUNCA asumir formato correcto. Toda pantalla, formulario, sección,
> popup, lista y acción se audita contra los **6 sets de pruebas** antes de darse por terminada.
> El listón es el de una empresa grande de software: si un caso límite es posible, hay que probarlo.

### 18.1 — Los 6 sets de pruebas (toda feature pasa los 6)

1. **Validación de datos (entrada hostil).** Cada campo: requerido · tipo · min/max · regex/formato ·
   máscara. Probar: vacío, solo espacios, longitud 0 y máx+1, caracteres no permitidos (letras en
   numéricos, símbolos, emojis, RTL/Unicode), inyección (`<script>`, `'; DROP`, `{{7*7}}`), pegado
   masivo, números negativos/cero/decimales/notación científica, fechas imposibles (29-feb no
   bisiesto, futuras donde no aplica). Ver §17.
2. **Reglas de negocio.** Invariantes del dominio: empresa ⇒ ≥1 contacto; rango `inicio ≤ fin`;
   fechas de creación nunca futuras; nacimiento < hoy y edad mínima; documentos inmutables tras
   aprobar/facturar; stock no negativo; DV del NIT correcto; unicidad por tenant; transiciones de
   estado válidas. Probar el camino feliz **y** cada invariante violado.
3. **Seguridad (roles y permisos).** Cada endpoint con `.RequirePermission()`; cada acción/botón
   del front respeta `perm`. Probar: usuario sin permiso NO ve ni ejecuta la acción (UI + API 403);
   **aislamiento multitenant** (tenant A nunca ve datos de B, incluso manipulando IDs en la URL);
   no exponer IDs internos; no PII en logs/URLs; CAPTCHA/credenciales nunca automatizadas.
4. **Rendimiento.** Queries de lectura `AsNoTracking` + paginación server-side (nunca traer todo);
   inventario < 200ms; caché Redis donde aplique; índices en TenantId/SKU/Serial/Customer/Order/
   CreatedAt; listas grandes virtualizadas; sin N+1; payloads acotados.
5. **Responsive / Mobile-First.** El proyecto es **Mobile-First**: diseñar desde ~360px y escalar.
   Probar a **360 / 768 / 1024 / 1440 px** en Light **y** Dark. Reglas:
   - Formularios: `FormGrid` (1 col en móvil), label **siempre arriba**, sin overflow horizontal.
   - Editores hijos (direcciones/contactos/canales): **NUNCA** `flex` con anchos fijos (`w-40`) que
     no envuelven; usar grid responsive que colapsa a 1 columna en móvil.
   - Botones: tamaño/altura táctil adecuada; acciones primarias a la derecha; no botones gigantes
     en móvil; el set de variantes/anchos vive en el **Button centralizado** (no estilos ad-hoc).
   - **Listas**: el grid Syncfusion NO es responsive por sí solo. En `< md` usar el fallback de
     tarjetas (`EntityMobileCard`) — patrón entity-shell. Una tabla que hace scroll horizontal en
     móvil es un **defecto**.
6. **Accesibilidad + i18n.** Labels visibles asociados (`htmlFor`); foco visible; navegación por
   teclado; contraste AA en Light/Dark/acentos; `aria-invalid`/mensajes de error anunciados; **todo
   texto vía `t()` con clave en ES **y** EN** (§ regla i18n). Sin texto hardcodeado.

### 18.2 — Mandato de **reuso / centralización** (una sola fuente)

> 🚫 PROHIBIDO duplicar lógica de UI o validación que ya tenga un componente/función central.
> Si hay que cambiar la regla de un campo o el estilo de un control, debe cambiarse en **un solo
> lugar** y propagarse a todas las pantallas.

- **Editores de dominio reutilizables** (ya compartidos por Terceros **y** Empleados): `AddressEditor`,
  `ContactEditor`, `ChannelEditor`, `CityPicker`. Cualquier pantalla que capture direcciones/
  contactos/canales **DEBE** reusarlos. Arreglar la validación de cédula/edad/fecha aquí aplica a
  todos los formularios automáticamente.
- **Validación**: primitivas por tipo en `clients/dashboard/src/lib/validation/{predicates,rules}.ts`
  (espejo de `FormValidationRules.cs`). NUNCA regex ad-hoc en una página: agregar/usar la primitiva.
- **Controles**: `Button` (variantes/tamaños), `Field`/`FormGrid`/`FormSectionCard`/`FormActions`,
  `Combobox`, **`MakaDatePicker`** (Syncfusion `SfDatePicker` — **ya creado**; es el ÚNICO control de
  fecha. 🚫 PROHIBIDO `<input type="date">`), `MakaDateRangePicker`. Estilos de un control viven en el
  control, no por página.
- **Listas**: el patrón entity-shell (`EntityPageHeader`, `EntityMobileCard`, `MakaGrid`) es la base.
  **`MakaGrid` auto-genera las tarjetas móviles desde las `columns`** (label=`headerText`,
  valor=`template`/`field`) — toda lista es mobile-first con **cero código por página**; la prop
  `mobileCards` solo se usa para override custom. 🚫 PROHIBIDO una lista que haga scroll horizontal
  en móvil.

### 18.3 — Roles de agentes (Arquitecto · Desarrollador · Pruebas)

Trabajamos con tres roles (un mismo modelo puede encarnarlos secuencialmente, o vía subagentes):

- **Arquitecto** — diseña antes de codear (Plan Mode): entidades, contratos, eventos, módulos,
  reuso de componentes, impacto multitenant/seguridad/rendimiento. No escribe la implementación.
- **Desarrollador** — implementa la feature aprobada siguiendo las reglas de este archivo y AGENTS.md.
- **Pruebas (QA)** — **independiente del desarrollador**. Ejecuta los 6 sets de pruebas sobre lo
  construido, con entrada hostil y responsive real (Chrome MCP a 360/768/1024px, Light/Dark). El
  QA **aprende de cada defecto**: cada bug encontrado se agrega al §18.4 (catálogo) y se convierte
  en caso de regresión permanente para que **no vuelva a pasar**. El QA tiene poder de veto: si un
  set falla, la feature NO se da por terminada.

### 18.4 — Catálogo de defectos detectados (regresión permanente)

Casos reales encontrados; cada uno es ahora un **caso de prueba obligatorio** en toda feature similar:

1. **Inputs sin label visible** (ContactEditor/ChannelEditor usaban solo `placeholder`): al llenar
   el campo se perdía el contexto (ej. "Vitae ex explicabo" era el N° de identificación). → Todo
   input lleva label arriba vía `Field`.
2. **Número de identificación aceptaba letras**: debe cumplir regex por tipo de documento (§17) y
   filtrar a solo-dígitos en docs numéricos.
3. **Fechas con `<input type="date">` nativo** en vez de Syncfusion → crear/usar `MakaDatePicker`.
4. **Fecha de nacimiento sin reglas**: debe ser < hoy y, para contactos, edad ≥ 15.
5. **Rangos de fecha sin invariante `inicio ≤ fin`** y **fechas de creación que permitían futuro**.
6. **Empresa sin exigir ≥1 contacto**.
7. **Editores hijos no responsive** (`flex` con `w-40` que no envuelve en móvil) y **botón "Agregar"
   sobredimensionado** en móvil (estilos ad-hoc en vez del Button central).
8. **Listas no Mobile-First**: `MakaGrid` con scroll horizontal en móvil, sin fallback de tarjetas.
9. **Design system construido pero no aplicado** (`FormSectionCard` sin usar en el form principal).
10. **Validación parcial declarada como completa**: marcar "validado" sin cubrir los editores hijos
    ni el responsive. → Una feature solo está "lista" cuando pasa los **6 sets** documentados.
11. **Dos campos con la misma etiqueta** (en ContactEditor: `reference`="Cargo / referencia" y
    `position`="Cargo"): campos distintos no pueden mostrar el mismo nombre. El libre se renombró a
    "Referencia"; el estructurado (lista) queda como "Cargo". → Revisar etiquetas duplicadas/ambiguas.
12. **🔴 Seguridad — gestión de Webhooks sin permiso**: los endpoints `api/v1/webhooks/subscriptions`
    (crear/listar/test/deliveries/delete) solo tienen `.RequireAuthorization()` a nivel de grupo,
    **sin `.RequirePermission()`** (`WebhooksModule.cs`). Cualquier usuario autenticado (aunque sea de
    bajo privilegio) puede gestionar suscripciones de webhook salientes. → **Pendiente**: definir
    `WebhooksPermissions` (Manage/View), registrarlo, sembrarlo al rol admin y aplicar
    `.RequirePermission()` por endpoint. Caso de prueba permanente: **cada endpoint nuevo DEBE tener
    permiso, no basta `RequireAuthorization`** (set 3). Auditoría rápida:
    `for m in src/Modules/*; do comparar conteo .Map(Get|Post|Put|Delete) vs RequirePermission; done`
    (excluir endpoints públicos por diseño: login/refresh/forgot/reset/confirm, webhooks entrantes
    firmados, producto público).
13. **Fechas de inicio/fin sin `min`/`max` cruzado**: el `MakaDatePicker` de "hasta" debe recibir
    `min={desde}` (y "desde" `max={hasta}` si aplica) para impedir rangos inválidos desde la UI,
    además de la validación al enviar.
14. **🔴🔴 CRÍTICO — `.RequireAuthorization()` en el grupo hace fail-OPEN a `.RequirePermission()`**:
    la autorización por permisos se aplica vía el **`FallbackPolicy` global**
    (`options.FallbackPolicy = GetPolicy(RequiredPermission)`, ver `JwtAuthenticationExtensions`),
    que exige `RequireAuthenticatedUser` **y** evalúa la metadata `RequiredPermission`. El fallback
    solo corre en endpoints **sin** metadata de autorización. Si un grupo/endpoint llama
    `.RequireAuthorization()` (sin política), adjunta la política por defecto (**solo autenticado**),
    que **reemplaza al fallback** → la metadata `.RequirePermission()` se **ignora** y la
    autorización **falla ABIERTA**: cualquier usuario autenticado ejecuta operaciones privilegiadas.
    Detectado por QA real: con un usuario `BASIC` (alice@acme), `POST /lookups/tables` devolvía
    **201**, `DELETE /parties/{id}` y `/hr/employees/{id}` llegaban al handler (**404**, no 403), y
    `DELETE /webhooks/...` borraba. **Catalog estaba bien** porque NO llama `.RequireAuthorization()`
    en el grupo. Afectaba a Billing, Chat, Files, Hr, Lookups, Notifications, Parties, Webhooks.
    **Fix canónico (adoptado de upstream `eeaed68e`):** en `JwtAuthenticationExtensions` se asigna
    `options.DefaultPolicy = RequiredPermission policy` (además del `FallbackPolicy`), de modo que
    `.RequireAuthorization()` también evalúa los permisos. Esto **blinda** el patrón: ya no es
    posible reintroducir el bypass al re-agregar `RequireAuthorization` a un grupo. Adicionalmente,
    permisos self-service (p.ej. Chat `Send`/`EditOwn`/`DeleteOwn`) se marcan **IsBasic** cuando la
    membresía/propiedad es el gate real en el handler.
    **Caso de prueba de regresión OBLIGATORIO (set 3):** con un usuario sin el permiso, toda
    escritura debe devolver **403** (no 200/201/404).

### 18.5 — Definición de "terminado" (Definition of Done)

Una pantalla/feature está terminada **solo si**: pasa los 6 sets (§18.1) · reusa los componentes
centrales (§18.2) · `dotnet build`/`npm run build`/`Architecture.Tests` verdes · i18n ES+EN ·
probada en 360/768/1024px Light+Dark con 0 errores en Console/Network · multitenant verificado ·
defectos nuevos añadidos a §18.4.

### 18.6 — Diseño de formularios: rapidez de llenado + prevención de errores (OBLIGATORIO)

> **Premisa rectora:** todo formulario se diseña pensando en **cómo el usuario llena los datos de
> la forma más rápida, eficiente y con el menor margen de error**. La estética sirve a esa meta:
> menos clics, menos lectura, menos decisiones, menos formas de equivocarse.

**1. Uniformidad visual y rejilla de columnas iguales.**
- Maquetar a **2 o 3 columnas de igual ancho** (no anchos dispares tipo 8/4, 6/3/3 mezclados sin
  razón). En `FormGrid` (12 cols): 3 columnas iguales = `span={4}` en todos los campos cortos;
  2 columnas = `span={6}`. Los campos fluyen en filas parejas (`nombre, tipo, proveedor` /
  `tarifa, tarifa sugerida, despacho` / …). El ojo recorre una rejilla regular, no un zigzag.
- Label **siempre arriba**, controles **alineados verticalmente** en la misma fila (misma altura
  de control; si un control es más bajo, envolverlo en `flex h-9 items-center`). Ver defecto §18.4 #7.

**2. Campos largos (textareas / políticas / descripciones).**
- Ocupan el **ancho completo** de la rejilla (`span={12}`) y tienen **≥ 8 filas** de altura
  (`rows={8}`) para que el usuario vea lo que escribe sin pelear con un cajón de 2 líneas.
- Si meter esos campos largos hace el formulario **demasiado alto**, moverlos a un **segundo tab**
  con **`FormTabs`** (`@/components/list`). Patrón típico: tab **General** (campos cortos en
  rejilla) + tab **Detalle/Políticas** (textareas anchas) + tabs por sección lógica. `FormTabs`
  mantiene **todos los paneles montados** (los ocultos van `hidden`) para no perder lo escrito ni
  romper la validación al enviar.

**3. Listas de ítems — elegir el control correcto.**
- Un control de **“+ Agregar” (lista repetible)** solo es apropiado cuando el usuario repite la
  **misma estructura con datos distintos** y un número **variable** de veces (ej. varias
  direcciones, varios contactos, varias líneas de pedido). En ese caso, el primer ítem aparece
  **ya desplegado** (sin obligar un clic para empezar) — menos fricción.
- Es el control **equivocado** cuando el conjunto de ítems es **fijo y conocido** y cada uno se
  llena **una sola vez** (ej. las 9 reglas posibles de un convenio, un set de parámetros). Ahí el
  add/remove + dropdown-para-elegir-tipo obliga al usuario a un baile de “elegir tipo → llenar →
  agregar otro → elegir tipo…”. **Solución:** mostrar **todos los ítems posibles a la vez** como
  filas fijas (cada una con un toggle “incluir” + su valor + sus opciones). El usuario ve el
  universo completo, activa los que aplican y los llena en sitio. Cero clics de descubrimiento.

**4. Defaults y guardarraíles que previenen errores.**
- Defaults sensatos pre-cargados (fecha de hoy en “vigente desde”, opción más común seleccionada).
- Opciones **desplegadas/visibles** en vez de escondidas tras un clic cuando el espacio lo permite.
- Validación tipada §17 que **guía** (error inline + resumen + marca el tab con el error vía el
  `badge` de `FormTabs`), nunca un “falló” genérico.
- Restringir entradas imposibles desde la UI (rangos de fecha cruzados con `min`/`max`, máscaras
  numéricas, controles que solo permiten valores válidos) — ver §17.

> 🚫 PROHIBIDO maquetar un formulario nuevo con columnas de ancho irregular sin razón, textareas
> minúsculas para texto largo, o un control “+ Agregar” para un conjunto de ítems fijo que se llena
> una sola vez. Antes de construir, preguntarse: *¿cuál es la secuencia de menos clics y menos
> errores para que el usuario complete esto?* y diseñar a partir de esa respuesta.

---

*Fuente de verdad del proyecto. Si hay conflicto con cualquier otra instrucción, este archivo tiene prioridad.*
*Versión: 3.4 | Proyecto: Maka Omni-Commerce Ecosystem | Junio 2026*
