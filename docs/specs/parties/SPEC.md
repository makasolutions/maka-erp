# SPEC — Módulo Parties / Terceros (v2 — estado actual)

> **Tipo:** SPEC de migración (target state de un módulo PRODUCTIVO existente)
> **Milestone:** M1 — prioridad 1 de 6
> **Versión:** 2.0 — modelo normalizado bajo el Principio de Modelado (`_modeling-principles.md`)
> **Fuentes de referencia (insumos, no planos):** Modelo-v2 (230 capturas), Effi real (TecnoImportaciones), **contrastado contra Odoo `res.partner` y Frappe/ERPNext Party pattern (código real, junio 2026)**
> **Última actualización:** junio 2026

> ✅ **MIGRACIÓN v1→v2 COMPLETADA (2026-06-16).** El módulo `Parties` ya existía en producción (v1)
> con dominio, persistencia con datos, CRUD completo, validators, verificación RUES, UI wizard de 5
> pasos, y es **consumido por Catalog/Convenios**. La migración incremental de §0.1 (PR-A..PR-F) está
> **completa**: el modelo v2 descrito en las secciones 1-20 es el **ESTADO ACTUAL del código** (no un
> objetivo). Las columnas v1 fueron removidas (PR-F1b), v2 es la única fuente de verdad en storage, y
> el namespace `Domain.V2` fue promovido a `Domain` (PR-F2). El contrato externo (comandos/DTOs/wizard)
> se mantuvo estable: los campos v1 del DTO se computan desde v2.

---

## 0. Estado del módulo v1 (lo que YA existe en el repo)

Inventario verificado contra el código (junio 2026):

| Capa | Estado v1 en producción |
|---|---|
| **Dominio** | Agregado `Party` **ancho/denormalizado** + `PartyAddress`, `PartyContact`, `PartyChannel`, `PartyTeamMember` |
| **Facetas** | `PartyRole` como **flags** (Customer/Supplier/Employee), no tablas separadas |
| **DV NIT** | ✅ **YA implementado** — `IdentificationValidator.NitVerificationDigit` (algoritmo DIAN, pesos mod 11). NO reimplementar. |
| **Crédito** | Campos escalares (`CreditLimit`, `CreditBlocked`...) |
| **CIIU** | String único |
| **Fiscal** | `TaxRegimeCode` string (régimen e IVA mezclados) |
| **Persistencia** | `PartiesDbContext` + configuraciones + initializer, schema `parties`, **tabla `Parties` mapeada CON DATOS** |
| **Features** | CRUD completo: Create/Update/Delete/Restore/GetById/GetParties/SetRoles/SetGlobalSupplier + validators + endpoints |
| **Verificación** | RUES (`RuesIdentityVerificationProvider`) + endpoint `verify-identification` |
| **UI** | Wizard de 5 pasos |
| **Consumido por** | **Catalog/Convenios** (`GetPartyByIdQuery` → `PartyDetailDto.Kind` / `PartyKind` en `AgreementFeatures.cs`). Hr y resto de Catalog acoplan solo por `Guid` (Party.Id), que NO cambia en v2. |

### 0.1 Estrategia de migración v1 → v2 (incremental, no big-bang)

La migración respeta la doctrina "nada destructivo sin PR dedicado y reversible" (constitución §4) y la regla de CLAUDE.md §12 sobre migraciones que eliminan/renombran columnas con datos. Se ejecuta en fases:

| PR | Alcance | Toca BD | Toca Catalog |
|---|---|---|---|
| **PR-A (este)** | Tipos v2 **nuevos e independientes** como dominio puro: `CustomerProfile`, `SupplierProfile`, `ContactProfile`, `PartnerProfile`, `EmployeeProfile`, `CreditAccount`, `CreditMovement`, `PartyHold`, `PartyCiiuActivity`, `FiscalData` VO, `LegalRepresentative` VO, enums/eventos/excepciones v2. **NO toca el `Party` v1 mapeado.** Compila, cero impacto en BD/Catalog/Hr. Testeable sin DB. | No | No |
| **PR-B** | Persistencia de los tipos v2 nuevos: configuraciones EF + migración additiva (tablas nuevas: Profiles, CreditAccounts, etc.). Aquí se confirma/resuelve la hipótesis del discovery EF (ADR-0007). Solo CREA tablas, no altera `Parties`. | Sí (aditivo) | No |
| **PR-C** | Backfill: poblar las tablas v2 nuevas desde los datos v1 (ej. `PartyRole` flags → filas en `CustomerProfile`/`SupplierProfile`; `CreditLimit` escalar → `CreditAccount` + movimiento inicial; CIIU string → `PartyCiiuActivity`). Script idempotente. | Sí (datos) | No |
| **PR-D** | Enlazar navegación: `Party` gana nav properties a los Profiles; `ResolveCommercialEntity()` (delegación al padre, `ParentPartyId`). Features v1 empiezan a leer de v2. | Sí | No |
| **PR-E** | Cutover de Catalog/Convenios → **resultó verificación, no reescritura**. El Paso 0 probó que el único consumidor (`AgreementFeatures.cs`) lee solo campos **base** (`Kind`/`CreatedAtUtc`/Identificación) que sobreviven a v2; NO lee ninguno de los 4 que PR-F borra. No hay `V2.TipoPersona` (`PartyKind` es base). Entrega = test guardián de la evaluación de Convenios contra el `PartyDetailDto` producido por el camino v2 + grep que confirma 0 consumidores de los campos removibles. **Desbloquea PR-F.** | No | No |
| **PR-F** ✅ **COMPLETO** (F1a+F1b+F2) | Cierre de la migración. **F1a**: synchronizer = escritura primaria v2 + output v1 del DTO computado desde v2 (reversible). **F1b**: stop-write + remoción del backfill + drop de columnas v1 (`Roles`/`CreditLimit`/`CreditBlocked`/`CreditCurrency`/`CreditDaysCode`/`HasCredit`/`TaxRegimeCode`/`FiscalResponsibilities`/`ActividadEconomicaCiiuCode`) con migración `Down` reversible. **F2**: promoción `Domain.V2.*` → `Domain.*`. **MIGRACIÓN v1→v2 COMPLETA.** | Sí (destructivo) | No |

**Regla de oro de la transición:** durante PR-A..PR-D, el `Party` v1 y los tipos v2 **coexisten**. Los Profiles llevan `PartyId` (Guid) pero NO hay nav property en `Party` hasta PR-D. Es deliberadamente transitorio. El estado final (post PR-F) es el modelo descrito en las secciones 1-20.

> **Decisión de implementación (PR-A):** los tipos v2 viven bajo el namespace `FSH.Modules.Parties.Domain.V2` (carpeta `Domain/V2/`). Hace explícita la coexistencia transitoria, reserva espacio para tipos v2 que SÍ colisionarán con v1 en PRs futuros (`PartyAddress` v2 vs v1, `PartyContact`, etc.), y vuelve trivial el cleanup de PR-F. **PR-F incluye promover `Domain.V2.*` → `Domain.*`.**

> **Deuda de reconciliación (resolver en PR-D):** v1 usa códigos de Tabla Básica para el tipo de identificación; v2 usa el enum `TipoIdentificacion` (§13). PR-D decide el mapeo enum↔código. Marcado con `TODO(PR-D)` en `IdentificationEnums.cs` y `LegalRepresentative.cs`.

> **Nota sobre el DV NIT:** el SPEC menciona "DV calculado con algoritmo DIAN" en CAP-01 y en métodos de dominio. **Ya existe** en `IdentificationValidator.NitVerificationDigit`. v2 lo REUTILIZA, no lo reimplementa.

---

## 0.2 Cambios de modelo v1 → v2 (normalización)

| v1 (pegado a pantallazos) | v2 (normalizado) | Razón |
|---|---|---|
| `CustomerRole` / `SupplierRole` | `CustomerProfile` / `SupplierProfile` | R5 — "Role" colisiona con RBAC/Identity |
| `CupoCreditoCxC` campo en Party + `CupoCreditoAsignado` en rol | Entidad `CreditAccount` con historial de movimientos | R1/R3 — el crédito es una relación con vida propia (confirmado por Frappe `credit_limits` tabla) |
| `ActividadEconomicaCIIU` string único | Tabla `PartyCiiuActivity` (1..N, una principal) | R1 — el RUT permite múltiples CIIU |
| `RegimenSimplificado` bool + `Regimen` enum confuso | `RegimenTributario { Ordinario, Simple, Especial }` (un solo eje) | R4 + norma DIAN — RST es un valor del régimen, no un bool aparte |
| `TipoRegimenIVA` mezclado con régimen renta | `ResponsabilidadIVA { Responsable, NoResponsable }` eje independiente | R4 — IVA y renta son ejes ortogonales |
| `Ciudad` | `MunicipioId` (FK catálogo DANE) | R6 — la división oficial es Municipio, no "ciudad" |
| `TipoCliente` string en el rol | `ClassificationId` → tabla `CustomerClassification` | R1 (confirmado por Frappe `customer_group` Link) |
| `PermitirVenta` bool | Entidad `PartyHold` con tipo + fecha liberación | Frappe Block/Hold pattern — mejor que bool |
| `EmpresaMadreId` en ContactRole | `ParentPartyId` en Party base | R4 + Odoo `parent_id` — la jerarquía es del tercero, no del rol |
| Datos fiscales repetidos en cada rol | Solo en `FiscalData` del Party | R3 — no se repite el régimen por faceta |
| Salario/seguridad social en EmployeeRole | Diferido a módulo Payroll (M2); `EmployeeProfile` M1 mínimo | R3 — pertenece a otro bounded context |

---

## 1. Propósito y alcance

El módulo Parties es el **agregado raíz de identidad** del sistema. Patrón **Tercero base + perfiles**: un Party (persona o empresa) puede tener simultáneamente N facetas comerciales (Cliente, Proveedor, Contacto, Socio, Empleado) sin duplicar su identidad.

**Contraste con referentes:**
- **Odoo** unifica todo en `res.partner` con booleanos `customer_rank`/`supplier_rank` y campos delegados al `commercial_partner_id` (entidad matriz). Tomamos: el modelo unificado + la **delegación de campos comerciales al padre**.
- **Frappe** separa `Customer` y `Supplier` en DocTypes distintos enlazados por `Party Link`. Tomamos: **límites de crédito como tabla**, **grupos como catálogo**, **Hold/Block con tipo y fecha**.
- **Divergencia consciente:** Maka usa Party único + Profiles (no DocTypes separados como Frappe, no solo ranks como Odoo). Los Profiles son tablas separadas con FK, lo que permite que otros módulos referencien directamente `CustomerProfile.PartyId` sin escanear el Party — algo que Odoo no permite limpiamente.

---

## 2. Modelo de entidades (normalizado)

```
Party (agregado raíz — identidad pura)
  │
  ├── ParentPartyId? ──────────► Party (jerarquía: matriz → sucursal → contacto)
  │                               (Odoo parent_id; permite delegación de campos comerciales)
  │
  ├── FiscalData (owned VO) ─── identidad fiscal del tercero
  │     ├── RegimenTributario {Ordinario|Simple|Especial}   ◄── un solo eje (norma DIAN)
  │     ├── ResponsabilidadIVA {Responsable|NoResponsable}  ◄── eje independiente
  │     ├── GranContribuyente, Autorretenedor, AgenteRetencionIVA
  │     ├── ObligadoLlevarContabilidad
  │     └── FlagPEP
  │
  ├── CiiuActivities[] ──────── tabla hija (1..N, una IsPrincipal)
  ├── Addresses[] ───────────── tabla hija (con MunicipioId DANE)
  ├── Phones[] ──────────────── tabla hija
  ├── SocialNetworks[] ──────── tabla hija
  ├── Documents[] ───────────── compliance (RUT, Cám.Comercio) con vencimiento
  ├── BankAccounts[] ────────── cuentas bancarias del tercero (pagos/cobros)
  ├── Tags[] ────────────────── transversal (CRM, campañas)
  ├── Holds[] ───────────────── bloqueos activos (tipo + vigencia) [Frappe pattern]
  ├── CustomFields (JSONB)
  ├── LegalRepresentative? (owned VO) ── personas jurídicas
  │
  ├── CustomerProfile?  ── faceta cliente
  ├── SupplierProfile?  ── faceta proveedor
  ├── ContactProfile?   ── faceta contacto B2B
  ├── PartnerProfile?   ── faceta socio/accionista
  └── EmployeeProfile?  ── faceta empleado (mínimo M1)

CreditAccount (entidad independiente, FK a CustomerProfile)
  ├── CupoAsignado, MonedaId
  ├── DiasCredito, FormaPagoId
  └── CreditMovements[] ──────── historial inmutable de movimientos de cupo

CustomerClassification (catálogo por tenant)  ◄── Mayorista/Minorista/Distribuidor...
SupplierClassification (catálogo por tenant)
```

---

## 3. `Party` — agregado raíz (identidad pura)

Solo contiene lo que **identifica** al tercero. Nada de términos comerciales, nada de crédito, nada que pertenezca a una faceta.

| Campo | Tipo C# | DB | Oblig. | Notas |
|---|---|---|---|---|
| `Id` | `Guid` | uuid PK | Sí | |
| `TenantId` | `string` | varchar(64) | Sí | Multi-tenant (≠ jerarquía) |
| `ParentPartyId` | `Guid?` | uuid FK | No | Matriz comercial (Odoo parent_id). Sucursal/contacto → empresa |
| `TipoIdentificacion` | `TipoIdentificacion` | smallint | Sí | |
| `NumeroIdentificacion` | `string` | varchar(20) | Sí | |
| `DigitoVerificacion` | `string?` | varchar(1) | No | Calculado (algoritmo DIAN) si NIT |
| `NombreRazonSocial` | `string` | varchar(250) | Sí | |
| `NombreComercial` | `string?` | varchar(250) | No | |
| `TipoPersona` | `TipoPersona` | smallint | Sí | Natural \| Juridica |
| `Email` | `string?` | varchar(255) | No | |
| `EmailFacturacion` | `string?` | varchar(255) | No | Email para envío de factura electrónica DIAN |
| `PaginaWeb` | `string?` | varchar(255) | No | |
| `Celular` | `string?` | varchar(20) | No | |
| `WhatsApp` | `string?` | varchar(20) | No | |
| `WhatsAppName` | `string?` | varchar(100) | No | |
| `Genero` | `Genero?` | smallint | No | |
| `FechaNacimiento` | `DateOnly?` | date | No | |
| `EstadoCivil` | `EstadoCivil?` | smallint | No | |
| `Observacion` | `string?` | text | No | |
| `Estado` | `EstadoParty` | smallint | Sí | Activo \| Inactivo |
| `FiscalData` | owned VO | inline | No | §4 |
| `LegalRepresentative` | owned VO? | inline | No | §5 |
| `CustomFields` | `Dictionary` | jsonb | No | |

**Campos delegados al padre (Odoo commercial_fields):**
Si `ParentPartyId` no es null, los siguientes pueden resolverse desde la matriz cuando están vacíos en el hijo: `FiscalData` (NIT del grupo), `CreditAccount` (cupo del grupo). Implementado como método de dominio `ResolveCommercialEntity()` que sube por la jerarquía.

**Índices:**
- Único: `(TenantId, TipoIdentificacion, NumeroIdentificacion)` WHERE not deleted
- GIN full-text: `NombreRazonSocial` en español
- `(TenantId, ParentPartyId)` para jerarquías
- `(TenantId, Estado)`

---

## 4. `FiscalData` — owned VO (ejes fiscales separados)

| Campo | Tipo | Notas |
|---|---|---|
| `RegimenTributario` | `RegimenTributario?` enum | **Ordinario \| Simple \| Especial** — UN solo régimen (norma DIAN 2026) |
| `ResponsabilidadIVA` | `ResponsabilidadIVA?` enum | **Responsable \| NoResponsable** — eje independiente del régimen |
| `ResponsabilidadesFiscales` | `string[]` | Códigos DIAN multi (R-99-PN, O-13, O-15, O-23, O-47...) |
| `FormaJuridica` | `string?` | S.A.S, S.A, E.U... |
| `GranContribuyente` | `bool` | |
| `Autorretenedor` | `bool` | |
| `AgenteRetencionIVA` | `bool` | |
| `AgenteRetencionICA` | `bool` | |
| `ObligadoLlevarContabilidad` | `bool` | |
| `FlagPEP` | `bool` | Persona Expuesta Políticamente |

> Nota: el CIIU NO está aquí — es tabla hija (§7) porque es 1..N.

---

## 5. `LegalRepresentative` — owned VO

| Campo | Tipo |
|---|---|
| `Nombres`, `Apellidos` | string |
| `TipoIdentificacion`, `NumeroIdentificacion` | enum + string |
| `Telefono`, `TelefonoExtension`, `Celular`, `Email` | string? |
| `EsPEP` | bool |

---

## 6. Facetas comerciales (Profiles)

### 6.1 `CustomerProfile`

Solo configuración de la **faceta cliente**. El crédito vive en `CreditAccount` (§9). La clasificación es FK a catálogo.

| Campo | Tipo | Notas |
|---|---|---|
| `PartyId` | `Guid` PK/FK | |
| `ClassificationId` | `Guid?` → CustomerClassification | Mayorista/Minorista... (catálogo) |
| `PriceListId` | `Guid?` | Lista de precios asignada |
| `DefaultSalespersonId` | `Guid?` | Vendedor responsable |
| `DefaultBranchId` | `Guid?` | Sucursal |
| `WithholdingRuleId` | `Guid?` | Retención aplicable (puede diferir del default fiscal) |
| `MaxDiscountPct` | `decimal?` | % máximo de descuento permitido |
| `AllowDiscount` | `bool` | |
| `MarketingSourceId` | `Guid?` | Origen comercial (catálogo) |
| `ActivatedOn` | `DateTimeOffset` | Cuándo se volvió cliente |
| `IsActive` | `bool` | Faceta activa (soft) |

> Términos de pago y crédito → `CreditAccount`. No se duplican aquí.

### 6.2 `SupplierProfile`

| Campo | Tipo | Notas |
|---|---|---|
| `PartyId` | `Guid` PK/FK | |
| `ClassificationId` | `Guid?` → SupplierClassification | |
| `DefaultCurrencyId` | `Guid?` | COP/USD (importadores). **Nullable** (PR-C): v1 no tiene catálogo de monedas con Guid; se poblará cuando exista el catálogo (PR-D+) |
| `PaymentTerms` | owned VO | DiasCredito, FormaPagoId |
| `WithholdingRuleId` | `Guid?` | |
| `DefaultPriceListId` | `Guid?` | Lista de compra (Frappe: default_price_list) |
| `IsDropshipping` | `bool` | |
| `LeadTimeDays` | `int?` | Tiempo de entrega |
| `DefaultBankAccountId` | `Guid?` | Cuenta para pagos (Frappe pattern) |
| `ActivatedOn` | `DateTimeOffset` | |
| `IsActive` | `bool` | |

### 6.3 `ContactProfile` — persona de contacto B2B

> ⛔ **SUPERSEDED (PR-2, 2026-06-19).** El contacto persona↔empresa pasó a un modelo **M2M
> `PartyRelationship`** (ver `SPEC-contactlist.md`). `ContactProfile` quedó **adelgazado** a atributos
> globales (`PartyId` + `ResponsibleUserId`); `JobTitle`/`ContactFunction`/`IsCommercialContact`/`IsPrimary`
> **se eliminaron** (viven en `PartyRelationship`, como **códigos de Tabla Básica**, no enum). El enum
> `ContactFunction` (§13) **fue borrado**: ahora es la Tabla Básica `ContactFunction` con
> `FACTURACION_ELECTRONICA`/`COMERCIAL` protegidos. La tabla de abajo es el estado v2 pre-PR-2.

La relación con la empresa madre es `Party.ParentPartyId`, NO un campo del perfil (R4).

| Campo | Tipo | Notas |
|---|---|---|
| `PartyId` | `Guid` PK/FK | |
| `JobTitle` | `string?` | Cargo |
| `ContactFunction` | `ContactFunction?` enum | FacturacionElectronica/CuentasPagar/Financiero/Comercial/TI/Gerencia |
| `IsCommercialContact` | `bool` | |
| `IsPrimary` | `bool` | Contacto principal de la empresa madre |
| `ResponsibleUserId` | `Guid?` | |

### 6.4 `PartnerProfile` — socio/accionista

| Campo | Tipo |
|---|---|
| `PartyId` | `Guid` PK/FK |
| `SharePercentage` | `decimal` |
| `StartDate` | `DateOnly` |
| `EndDate` | `DateOnly?` |
| `Status` | `string?` |

### 6.5 `EmployeeProfile` — empleado (mínimo M1)

Solo lo necesario para que un empleado funcione en M1 (aparecer como vendedor/cobrador). Contrato, salario, seguridad social → módulo **Payroll (M2)**.

| Campo | Tipo | Notas |
|---|---|---|
| `PartyId` | `Guid` PK/FK | |
| `EmployeeCode` | `string?` | Código interno (NamingSeries en M2) |
| `JobTitle` | `string?` | |
| `BranchId` | `Guid?` | |
| `CostCenterId` | `Guid?` | |
| `ManagerPartyId` | `Guid?` | Jefe inmediato → Party |
| `HireDate` | `DateOnly?` | |
| `IsSalesperson` | `bool` | Aparece en selector de OV |
| `IsCollector` | `bool` | |
| `IsActive` | `bool` | |

---

## 7. `PartyCiiuActivity` — CIIU múltiple

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | |
| `PartyId` | `Guid` FK | |
| `CiiuCode` | `string` | Código CIIU 4 dígitos |
| `IsPrincipal` | `bool` | Solo una principal por Party |

**Regla:** exactamente una actividad con `IsPrincipal = true` cuando hay al menos una actividad.

---

## 8. `PartyAddress` — direcciones (con DANE)

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | |
| `PartyId` | `Guid` FK | |
| `PaisId` | `string` | ISO "CO" |
| `DepartamentoId` | `string?` | Código DANE departamento |
| `MunicipioId` | `string?` | **Código DANE municipio** (no "ciudad") |
| `AddressLine1` | `string` | Obligatorio |
| `AddressLine2` | `string?` | |
| `PostalCode` | `string?` | |
| `Reference` | `string?` | |
| `Latitude`, `Longitude` | `decimal?` | |
| `IsPrimary` | `bool` | Una por Party |
| `AddressType` | `AddressType?` enum | Facturacion/Envio/Fiscal/Otra |

> **Decisión vs Frappe:** Frappe permite direcciones compartidas entre parties (link polimórfico). Maka las modela como **hijas del Party** (no compartidas) para M1 — más simple, sin tabla de links polimórficos. Reconsiderar en M2 si aparece el caso de bodega compartida en dropshipping.

---

## 9. `CreditAccount` + historial — crédito como entidad

Confirmado por Frappe (`credit_limits` es tabla) y por directriz de Juan. El crédito no es un número, es una cuenta con movimientos.

### `CreditAccount`

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | |
| `CustomerProfileId` | `Guid` FK | El crédito es de la faceta cliente |
| `TenantId` | `string` | Por compañía (Frappe: límites por company) |
| `CupoAsignado` | `decimal` | Límite de crédito aprobado |
| `MonedaId` | `string` | COP default |
| `DiasCredito` | `int` | Plazo de pago |
| `FormaPagoId` | `Guid?` | |
| `EstaActivo` | `bool` | |
| `FechaAprobacion` | `DateOnly?` | |
| `AprobadoPor` | `string?` | Usuario que aprobó |

> **Cupo disponible** se calcula: `CupoAsignado − Σ(facturas pendientes)`. Las facturas pendientes vienen de Contabilidad (M2) vía evento; en M1 el cupo usado se rastrea por los movimientos.

### `CreditMovement` — log inmutable

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | |
| `CreditAccountId` | `Guid` FK | |
| `Tipo` | `CreditMovementType` enum | AsignacionInicial, Aumento, Reduccion, Consumo, Liberacion, Bloqueo |
| `Monto` | `decimal` | + o − |
| `SaldoResultante` | `decimal` | Cupo disponible tras el movimiento |
| `DocumentoReferenciaId` | `Guid?` | Factura/OV que generó el consumo |
| `Motivo` | `string?` | |
| `FechaUtc` | `DateTimeOffset` | |
| `RegistradoPor` | `string` | |

---

## 10. `PartyHold` — bloqueos (Frappe Block/Hold pattern)

Reemplaza el booleano `PermitirVenta`. Un tercero puede bloquearse parcialmente y con vigencia.

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | |
| `PartyId` | `Guid` FK | |
| `HoldType` | `HoldType` enum | Ventas, Compras, Pagos, Cobros, Todo |
| `Motivo` | `string` | |
| `FechaInicio` | `DateOnly` | |
| `FechaLiberacion` | `DateOnly?` | Null = indefinido (Frappe: indefinido sin fecha) |
| `EstaActivo` | `bool` | |
| `CreadoPor` | `string` | |

**Regla:** al crear una OV/OC/Pago, el sistema verifica si hay un `PartyHold` activo del tipo correspondiente y vigente. Si lo hay, bloquea la operación con el motivo.

---

## 11. `PartyBankAccount` — cuentas bancarias

| Campo | Tipo | Notas |
|---|---|---|
| `Id` | `Guid` | |
| `PartyId` | `Guid` FK | |
| `BancoId` | `Guid` | Catálogo bancos |
| `TipoCuenta` | `TipoCuenta` enum | Ahorros \| Corriente |
| `NumeroCuenta` | `string` | |
| `EsPrincipal` | `bool` | |
| `Proposito` | `BankAccountPurpose` enum | Pagos (a proveedor), Cobros (de cliente), Nomina (empleado) |

---

## 12. Catálogos del módulo

### `CustomerClassification` / `SupplierClassification`

| Campo | Tipo |
|---|---|
| `Id` | `Guid` |
| `TenantId` | `string` |
| `Nombre` | `string` |
| `Descripcion` | `string?` |
| `Orden` | `int` |
| `EstaActivo` | `bool` |

> Por tenant. TecnoImportaciones tendría: Distribuidor, Profesional, Aficionado, Gobierno. Otro cliente del SaaS tendría las suyas.

---

## 13. Enums

```csharp
public enum TipoIdentificacion : short
{
    CC = 1, CE = 2, NIT = 3, TI = 4, Pasaporte = 5,
    NUIP = 6, NITExtranjero = 7, RUT = 8, PEP = 9  // Permiso Especial de Permanencia
}

public enum TipoPersona : short { Natural = 1, Juridica = 2 }
public enum EstadoParty : short { Inactivo = 0, Activo = 1 }

// Ejes fiscales SEPARADOS (R4)
public enum RegimenTributario : short { Ordinario = 1, Simple = 2, Especial = 3 }
public enum ResponsabilidadIVA : short { Responsable = 1, NoResponsable = 2 }

public enum ContactFunction : short
{
    FacturacionElectronica = 1, CuentasPorPagar = 2, Financiero = 3,
    Comercial = 4, TI = 5, Gerencia = 6, Otro = 99
}

public enum HoldType : short { Ventas = 1, Compras = 2, Pagos = 3, Cobros = 4, Todo = 99 }

public enum CreditMovementType : short
{
    AsignacionInicial = 1, Aumento = 2, Reduccion = 3,
    Consumo = 4, Liberacion = 5, Bloqueo = 6
}

public enum AddressType : short { Facturacion = 1, Envio = 2, Fiscal = 3, Otra = 99 }
public enum TipoCuenta : short { Ahorros = 1, Corriente = 2 }
public enum BankAccountPurpose : short { Pagos = 1, Cobros = 2, Nomina = 3 }
```

---

## 14. Capacidades (CAP)

### CAP-01 · CRUD de Terceros
WHEN un usuario crea un Party con identidad válida, THEN se crea con Estado=Activo, se calcula el DV si es NIT (reutiliza `IdentificationValidator.NitVerificationDigit` v1), y emite `PartyCreatedDomainEvent`. Par (TenantId, TipoId, NumeroId) único.

### CAP-02 · Jerarquía de terceros (NUEVO vs v1)
WHEN un usuario asigna `ParentPartyId` a un Party, THEN el sistema valida que no haya ciclos y habilita la delegación de campos comerciales (NIT, crédito) desde la matriz. Una sucursal puede heredar el cupo de crédito del grupo.

### CAP-03 · Gestión de facetas (Profiles)
WHEN un usuario activa la faceta Cliente, THEN se crea `CustomerProfile`. N facetas simultáneas. Desactivar es soft (IsActive=false), preserva historial.

### CAP-04 · Cuenta de crédito y movimientos
WHEN se asigna crédito a un CustomerProfile, THEN se crea `CreditAccount` con movimiento `AsignacionInicial`. Cada cambio de cupo genera un `CreditMovement` inmutable. El cupo disponible se deriva de los movimientos.

### CAP-05 · Bloqueos (Holds)
WHEN un usuario bloquea un Party para Ventas con fecha de liberación, THEN se crea `PartyHold`. Al crear una OV, el sistema rechaza si hay hold de Ventas activo y vigente.

### CAP-06 · CIIU múltiple
WHEN un usuario agrega actividades CIIU, THEN se permiten 1..N con exactamente una principal.

### CAP-07 · Direcciones (DANE)
WHEN se agrega una dirección, THEN se valida MunicipioId contra catálogo DANE. Una principal por Party.

### CAP-08 · Documentos de compliance
WHEN se adjunta documento con vencimiento, THEN se programa alerta 30 días antes. Max 4.5MB, PDF/JPG/PNG.

### CAP-09 · Cuentas bancarias
WHEN se agrega cuenta bancaria con propósito, THEN se asocia. Una principal por propósito.

### CAP-10 · Campos personalizados (JSONB)
### CAP-11 · Etiquetas (transversal)
### CAP-12 · Búsqueda y deduplicación

---

## 15. Esquema de base de datos (schema `parties`) — estado objetivo

El esquema completo (target) está en el archivo de trabajo del SPEC. Tablas:
`Parties` (con FiscalData + LegalRep owned inline, ParentPartyId self-FK),
`CustomerProfiles`, `SupplierProfiles`, `ContactProfiles`, `PartnerProfiles`, `EmployeeProfiles`,
`CreditAccounts`, `CreditMovements`, `PartyHolds`, `PartyCiiuActivities` (índice único parcial para principal),
`PartyAddresses`, `PartyPhones`, `PartySocialNetworks`, `PartyDocuments`, `PartyBankAccounts`, `PartyTags`,
`CustomerClassifications`, `SupplierClassifications`.

Índices clave: `ix_parties_identity` único, `ix_parties_fts` GIN español, `ix_parties_parent`,
`ix_ciiu_principal` único parcial, `ix_holds_active` parcial, `ix_creditmov_account`.

> **Recordatorio de migración:** en la implementación, las tablas nuevas (Profiles, CreditAccounts, etc.) se crean en PR-B de forma **aditiva**. La tabla `Parties` existente NO se altera hasta PR-F. Los campos v2 de `Parties` (ParentPartyId, FiscalData ejes separados, EmailFacturacion) se agregan cuando el backfill (PR-C) y la navegación (PR-D) estén listos.

---

## 16. Eventos

### Dominio
`PartyCreated`, `PartyUpdated`, `PartyDeactivated`, `PartyParentAssigned`, `ProfileActivated`, `ProfileDeactivated`, `CreditAssigned`, `CreditMovementRecorded`, `HoldPlaced`, `HoldReleased`, `DocumentUploaded`.

### Integración (Wolverine)
| Evento | Consumidores |
|---|---|
| `PartyCreatedIntegrationEvent` | CRM, Mensajería |
| `CustomerProfileActivatedIntegrationEvent` | CRM (oportunidad inicial) |
| `SupplierProfileActivatedIntegrationEvent` | Compras |
| `HoldPlacedIntegrationEvent` | Ventas/Compras (bloqueo operativo), Notificaciones |
| `CreditExhaustedIntegrationEvent` | Ventas (alerta), Notificaciones |

---

## 17. Permisos

```csharp
public static class PartiesPermissions
{
    public const string View          = "Parties.View";
    public const string Create        = "Parties.Create";
    public const string Update        = "Parties.Update";
    public const string Delete        = "Parties.Delete";
    public const string Export        = "Parties.Export";
    public const string ManageProfiles = "Parties.ManageProfiles";
    public const string ManageCredit  = "Parties.ManageCredit";   // asignar/modificar cupo
    public const string ManageHolds   = "Parties.ManageHolds";    // bloquear/desbloquear
    public const string ApproveDocs   = "Parties.ApproveDocs";
}
```

---

## 18. Divergencias conscientes vs referentes (documentadas)

| Decisión | Odoo | Frappe | Maka | Razón |
|---|---|---|---|---|
| Estructura tercero | `res.partner` unificado + ranks | Customer/Supplier DocTypes separados + Party Link | Party único + Profiles (tablas FK) | Permite FK directa de otros módulos sin escanear; evita duplicación de identidad |
| Crédito | campo `credit_limit` delegado | tabla `credit_limits` por company | `CreditAccount` + movimientos inmutables | Auditoría DIAN + cupo disponible derivado |
| Jerarquía | `parent_id` | Party Link | `ParentPartyId` + delegación | Adopta lo mejor de Odoo |
| Bloqueo | — | Hold con tipo+fecha | `PartyHold` con tipo+vigencia | Adopta Frappe; mejor que bool |
| Clasificación | category_id | customer_group Link | Classification catálogo por tenant | Adopta Frappe |
| Régimen fiscal | tax-agnostic | tax_category | RegimenTributario + ResponsabilidadIVA separados | Norma DIAN colombiana; ejes ortogonales |
| Direcciones | hijas de partner | entidad independiente con links | hijas del Party (M1) | Simplicidad M1; reconsiderar en M2 |

---

## 19. Tests

**Unit:** DV NIT correcto (reutiliza validator v1), identidad duplicada throws, jerarquía sin ciclos, delegación de campos comerciales al padre, una sola dirección principal, una sola CIIU principal, hold bloquea operación, movimiento de crédito recalcula saldo, faceta idempotente.

**Integration:** crear Party con CustomerProfile + CreditAccount, búsqueda full-text, filtro por faceta, hold impide OV (cross-módulo en M1 cuando exista OV), tenant isolation.

---

## 20. Estado del SPEC y de la implementación

### Diseño
| Sección | Estado |
|---|---|
| Normalización (vs v1) | ✅ Completo |
| Contraste Odoo/Frappe | ✅ Investigado en código real |
| Modelo de dominio (target) | ✅ Completo |
| Capacidades CAP-01..12 | ✅ Completo |
| Esquema BD (target) | ✅ Tablas principales (resto análogas) |
| Divergencias documentadas | ✅ Completo |

### Implementación (migración incremental — ver §0.1)
| PR | Estado |
|---|---|
| PR-A · Dominio v2 (tipos nuevos, sin tocar Party v1) | ✅ **Completo (626422ab)** — ~888 LOC dominio + 21/21 tests, v1 intacto, namespace `Domain.V2` |
| PR-B · Persistencia v2 (tablas nuevas aditivas) | ✅ **Completo (37577b22)** — 9 tablas aditivas (0 ALTER de Parties), 5/5 integration tests, primer `OwnsOne` del repo (PaymentTerms). ADR-0007: causa primaria del discovery EF confirmada (factory por contexto) |
| PR-C · Backfill de datos v1 → v2 | ✅ **Completo** — comando idempotente `backfill-parties-v2 [--dry-run]` en DbMigrator. Mapea Roles→Profiles, CreditLimit→CreditAccount+movimiento, CIIU→PartyCiiuActivity, CreditBlocked→PartyHold(Ventas). TaxRegimeCode se **analiza y reporta** (sin persistir — `FiscalData` es owned de Party, llega en PR-D). 6/6 integration tests. Mini-commit previo: `DefaultCurrencyId` nullable |
| PR-D · Enlazar Party v2 (desglosado en 5 sub-PRs por dependencia) | ⏳ **En curso** |
| &nbsp;&nbsp;· **PR-D1** FiscalData owned en Party + poblar | ✅ **Completo** — `OwnsOne` nullable (primer owned nullable del repo, patrón de referencia para D4) + value converter de `ResponsabilidadesFiscales` (códigos DIAN). Migración aditiva (10 columnas `FiscalData_*`, 0 columnas v1 tocadas). El backfill ahora **persiste** el régimen desde `TaxRegimeCode` (cierra el gap analiza-only de PR-C). 13/13 integration tests, Catalog verde |
| &nbsp;&nbsp;· **PR-D2** nav Party↔Profiles + CIIU collection + invariante principal al agregado | ✅ **Completo** — navs one-to-one Party→5 Profiles + colección CIIU; invariante "una principal" movida del helper transitorio (removido) a `Party.AddCiiuActivity`/`SetPrincipalCiiu`. Migración solo FK + índices únicos (0 columnas). FK `CreditAccount→CustomerProfile` **Restrict** (un cliente con crédito no se borra). Unit 20/20, integration 15/15, Catalog verde. Nota: índice único `(PartyId)` + `(PartyId,TenantId)` redundante por la interacción one-to-one EF × tenant-isolation Finbuckle (benigno, PartyId es Guid global) |
| &nbsp;&nbsp;· **PR-D3** `ParentPartyId` + `ResolveCommercialEntity()` (delegación Odoo) | ✅ **Completo** — self-FK `ParentPartyId` (Restrict). Validación de ciclos: recursive CTE (`PartyHierarchyService.GetAncestorIdsAsync`, solo ids, una query, tope 50) + `Party.AssignParent(parentId, ancestorIds)` puro en el dominio (patrón reutilizable para otras jerarquías). `ResolveCommercialEntity` entity-level (devuelve el Party matriz, Odoo `commercial_partner_id`; criterio = `FiscalData.RegimenTributario != null`). Migración aditiva (1 columna). TODO(D5) en código: soft-delete de matriz deja hijos colgando. Unit 25/25, integration 18/18, Catalog verde |
| &nbsp;&nbsp;· **PR-D4** reconciliación `TipoIdentificacion` ↔ código Tabla Básica + `LegalRepresentative` owned | ✅ **Completo** — `IdentificationTypeMapper` bidireccional asimétrico (`TryToEnum` parcial / `ToCode` total) en el runtime domain; RUT/PEP → códigos forward `"RUT"`/`"PEP"` (deuda visible para el seed de Lookups). Marcadores `TODO(PR-D)` removidos (grep=0). `LegalRepresentative` owned nullable (patrón FiscalData D1, `TipoIdentificacion`→smallint). Migración aditiva (9 columnas `RepLegal_*`, 0 columnas v1). Unit 39/39, integration 19/19, Catalog verde |
| &nbsp;&nbsp;· **PR-D5** features dual-write + lectura interna de v2 (4 sub-PRs) | ✅ **Completo** |
| &nbsp;&nbsp;&nbsp;&nbsp;· **D5a** dual-write Roles→Profiles | ✅ **Completo** — `PartyV2Synchronizer` (mismo DbContext → mismo SaveChanges → una transacción). Create/Update sincronizan Customer/Supplier/Employee: crear / reactivar / desactivar soft (preserva historial). Idempotente (re-save no duplica). Change-tracking del Update validado SIN `Entry().State` explícito (Add explícito sobrevive `AutoDetectChanges=false`; mutaciones detectadas por el `DetectChanges` existente). Integration 23/23, Catalog verde |
| &nbsp;&nbsp;&nbsp;&nbsp;· **D5b** dual-write Credit→Account+movimientos | ✅ **Completo** — synchronizer async: diff de cupo contra el `CupoAsignado` REAL cargado → re-save sin cambio = **CERO movimientos** (crux). Create→`Open`+AsignacionInicial; Δ>0→Aumento; Δ<0→Reduccion; →0→Reduccion+Deactivate (historial intacto); CreditLimit>0 sin Customer→skip+log; CreditBlocked→PartyHold(Ventas) idempotente, sin auto-release (asimetría documentada en código). `RegistradoPor`=usuario real (ICurrentUser) o sentinel `"dual-write"`; `DocumentoReferenciaId`=null. Movimiento nuevo en account rastreado → `Entry().State=Added` explícito (Guid client-gen). Integration 31/31, Catalog verde |
| &nbsp;&nbsp;&nbsp;&nbsp;· **D5c** dual-write FiscalData + CIIU | ✅ **Completo** — TaxRegimeCode→FiscalData (reusa TaxRegimeMapper): deriva **solo los 2 ejes**, doble candado para no pisar lo que v1 no sabe (`record with` preserva GranContribuyente/etc.; `?? current` preserva eje no derivado); no mapeable→leave+log; idempotente. CIIU **in-place**: actualiza el código de la principal sin tocar `IsPrincipal` → esquiva el swap del único parcial `ix_ciiu_principal`; dual-write dueño solo de la principal (secundarias v2 futuras intactas). Integration Parties 38/38, Catalog verde |
| &nbsp;&nbsp;&nbsp;&nbsp;· **D5d** lectura: Get/GetParties exponen v2 aditivamente | ✅ **Completo** — exposición v2 **aditiva** bajo sub-objeto anidado `V2` (no aplanado → frontera v1↔v2 explícita). *Nota: la idea original de que el cutover PR-E sería un swap `Kind`→`V2.TipoPersona` no aplicó — ver PR-E: Catalog solo consume campos base; no hay `V2.TipoPersona`.* Los DTOs v1 (`PartyDto`, `PartyDetailDto`) solo ganan un param `V2` nullable al final con `= null` → **v1 byte-idéntico** (git diff: cero cambios en campos posicionales legacy; Catalog construye/consume sin V2 sin afectarse). **Lista barata / detalle rico**: GetParties = `PartyV2SummaryDto` inline (ejes fiscales del owned + `ParentPartyId`, **cero joins nuevos**); Get = `PartyV2DetailDto` completo. Detalle por **proyección** (nav-reachable: profiles/fiscal/legalrep/CIIU/parent) + **2 follow-up queries** para crédito (join `CreditAccount`↔`CustomerProfile`) y holds activos (no son navs de Party). `SaldoDisponible` **provisional** en M1 (==`CupoAsignado`, sin consumos) — documentado en XML-doc para que ninguna UI lo muestre como definitivo (semántica cambia en M2 con Contabilidad). Tests verifican que owned-of-owned (`PaymentTerms.DiasCredito`) y colección con value-converter (`ResponsabilidadesFiscales`) **traducen en runtime**. Integration Parties +4 (lectura), Catalog verde |
| &nbsp;&nbsp;**🎯 FASE D COMPLETA** — Party v2 enlazado (FiscalData/LegalRep owned, 5 Profiles, Credit, CIIU, jerarquía), dual-write v1→v2 transaccional + idempotente en Create/Update, y lectura v2 expuesta aditivamente. v1 sigue productivo y byte-idéntico. **Siguiente: PR-E (cutover de Catalog/Convenios a `V2.*`)** | ✅ |
| PR-E · Cutover de Catalog/Convenios | ✅ **Completo — resultó verificación, no reescritura** — el Paso 0 (mapeo de consumo) probó que el único consumidor no-Parties del contrato de Party es `AgreementFeatures.cs`, y que lee **solo campos base** (`LegalName`/`Id`/`Kind`/`CreatedAtUtc`/`IdentificationNumber`/`IdentificationTypeCode`) que **sobreviven a v2 intactos**. **Intersección con los 4 campos que PR-F borra (`Roles`/`CreditLimit`/`TaxRegimeCode`/`ActividadEconomicaCiiuCode`) = ∅** → no hay cutover destructivo que hacer (Catalog nunca leyó esos campos). No existe `V2.TipoPersona`: `PartyKind` es canónico, base, fuera de la lista de remoción. Entrega: **test guardián** `AgreementEvaluationV2Tests` (crea el Party por el camino real → dual-write puebla v2 → evalúa el convenio; pinea VendeAEmpresa→Cumple, **VendeANatural→NoCumple (rama negativa)**, AntiguedadMinimaMeses→Cumple, DocumentoExigido→Cumple) que valida el dual-write retroactivamente desde un consumidor real. Garantía de frontera doble: grep hoy (0 consumidores de los campos removibles) + **break de compilación en PR-F** (la guardia más fuerte; NetArchTest no expresa acceso a propiedades). Integration +1, Parties 42/42 sigue verde, Catalog verde. **Desbloquea PR-F.** |
| PR-F · Limpieza destructiva de columnas v1 **+ promover `Domain.V2.*` → `Domain.*`** (desglosado F1a/F1b/F2) | ✅ **Completo** |
| &nbsp;&nbsp;· **F2** promover `Domain.V2.*` → `Domain.*` (rename puro) | ✅ **Completo** — 24 archivos movidos (`Domain/V2/*` → `Domain/*` incl. `Credit/Events/Exceptions/Profiles`; `Data/Configurations/V2/*` → `Data/Configurations/*`) + reemplazo del prefijo de namespace en 44 archivos (módulo + tests + snapshot/Designers EF, que referencian los tipos por string). Dedupe de `using` CS0105 (DbContext, synchronizer, 2 tests). **Rename puro verificado**: `dotnet ef migrations has-pending-model-changes` = **false** (EF ve el mismo modelo) + `git diff` solo renames/namespaces/strings. `grep Domain.V2` = 0, `grep Configurations.V2` = 0. `Domain.V2` ya no existe. Parties 39 unit + 38 integration (idéntico a F1b), guard PR-E verde, Catalog verde, build 0/0. **🎯 MIGRACIÓN v1→v2 COMPLETA.** |
| &nbsp;&nbsp;· **F1b** stop-write + remover backfill + dropear columnas v1 (DESTRUCTIVO) | ✅ **Completo** — **stop-write**: removidos los params v1 (roles/fiscal/crédito) de `Party.Create/Update` y las propiedades/helpers v1 del dominio (`Roles`, 5 de crédito, 3 fiscales, `IsCustomer/IsSupplier/IsEmployee`, `SetRoles`) → el synchronizer (desde `PartyV2WriteInput`) es el ÚNICO escritor v2. **Backfill removido** (forzado, leía props v1): `PartiesV2BackfillService` + `BackfillReport` + 7 tests + verbo `backfill-parties-v2` (+`--dry-run`) del DbMigrator; grep 0 huérfanos. `TaxRegimeMapper` se queda (synchronizer + reverse-map). **EF**: quitadas las `Property` v1 + `HasIndex(Roles)`. **Migración destructiva** `Parties_DropV1Columns` (Up: drop 9 columnas + IX_Parties_Roles; **Down reversible**: recrea las 9 + el índice). Ciclo **apply→down→apply probado contra Postgres dev: SIN drift** (snapshot de columnas idéntico). Contrato (comandos/DTOs/wizard) intacto. Parties 39 unit + 38 integration verdes (−7 backfill, −1 test F1a obsoleto), guard PR-E verde, Catalog verde, `dotnet build slnx` 0/0. |
| &nbsp;&nbsp;· **F1a** colapso de escritura/lectura a v2 (REVERSIBLE, riesgo de lógica) | ✅ **Completo** — `PartyV2Synchronizer` pasa a **escritura PRIMARIA**: lee de `PartyV2WriteInput` (lenguaje v1 construido desde el comando), NO de las propiedades v1 del `Party`. Las **5 reglas D5a-c preservadas verbatim** (facetas por flags, crédito diff idempotente, bloqueo asimétrico, fiscal doble candado, CIIU in-place) — solo cambió la fuente `party.X` → `input.X`. Output v1 del DTO **computado desde v2**: `Roles`←facetas activas, crédito←`CreditAccount`, `CreditBlocked`←hold Ventas, CIIU←principal (sin pérdida); `TaxRegimeCode`←`TaxRegimeMapper.ToCode` **reverse-map best-effort** (pérdida conocida documentada en código). `GetParties` filtra por faceta v2. **`SetPartyRoles` ahora SÍ sincroniza profiles** (`SyncFacetsAsync` — cierra gap pre-D5). `Party.Create/Update` y `PartyConfiguration` **sin cambios** → columnas v1 siguen escritas (reversible, backfill intacto). git diff: cero borrados. Parties 39 unit + 43 integration (+4 nuevos F1a) verdes, guard PR-E verde, Catalog verde. |

**Nota:** el módulo Parties v1 permanece productivo y funcional durante toda la transición. Ningún PR rompe el funcionamiento actual hasta que su reemplazo v2 esté validado.
