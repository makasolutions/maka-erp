# SPEC — Módulo Parties / Terceros (v2 — normalizado)

> **Tipo:** Forward SPEC
> **Milestone:** M1 — prioridad 1 de 6
> **Versión:** 2.0 — reescrito bajo el Principio de Modelado (`_modeling-principles.md`)
> **Fuentes (insumos, no planos):** Modelo-v2 (230 capturas), Effi real, **contrastado contra Odoo `res.partner` y Frappe/ERPNext (código real, jun 2026)**
> **Última actualización:** junio 2026

---

## 0. Cambios respecto a v1 (normalización)

| v1 (pegado a pantallazos) | v2 (normalizado) | Razón |
|---|---|---|
| `CustomerRole` / `SupplierRole` | `CustomerProfile` / `SupplierProfile` | R5 — "Role" colisiona con RBAC/Identity |
| `CupoCreditoCxC` campo + `CupoCreditoAsignado` en rol | Entidad `CreditAccount` + movimientos | R1/R3 — crédito es relación con vida propia (Frappe `credit_limits` tabla) |
| `ActividadEconomicaCIIU` string único | Tabla `PartyCiiuActivity` (1..N, una principal) | R1 — RUT permite múltiples CIIU |
| `RegimenSimplificado` bool + `Regimen` confuso | `RegimenTributario {Ordinario,Simple,Especial}` | R4 + norma DIAN — RST es valor del régimen, no bool |
| `TipoRegimenIVA` mezclado | `ResponsabilidadIVA {Responsable,NoResponsable}` eje aparte | R4 — IVA y renta son ejes ortogonales |
| `Ciudad` | `MunicipioId` (FK catálogo DANE) | R6 — división oficial es Municipio |
| `TipoCliente` string | `ClassificationId` → `CustomerClassification` | R1 (Frappe `customer_group` Link) |
| `PermitirVenta` bool | Entidad `PartyHold` (tipo + fecha) | Frappe Block/Hold pattern |
| `EmpresaMadreId` en rol | `ParentPartyId` en Party base | R4 + Odoo `parent_id` |
| Datos fiscales repetidos por rol | Solo en `FiscalData` del Party | R3 |
| Salario/seg.social en EmployeeRole | Diferido a Payroll M2; `EmployeeProfile` mínimo | R3 — otro bounded context |

---

## 1. Propósito y alcance

Módulo Parties = **agregado raíz de identidad**. Patrón **Tercero base + perfiles**: un Party puede tener N facetas comerciales (Cliente, Proveedor, Contacto, Socio, Empleado) sin duplicar identidad.

**Contraste con referentes:**
- **Odoo** unifica en `res.partner` con `customer_rank`/`supplier_rank` y campos delegados a `commercial_partner_id`. Tomamos: modelo unificado + **delegación de campos comerciales al padre**.
- **Frappe** separa Customer/Supplier en DocTypes enlazados por Party Link. Tomamos: **crédito como tabla**, **grupos como catálogo**, **Hold/Block con tipo y fecha**.
- **Divergencia consciente:** Party único + Profiles como tablas FK separadas → otros módulos referencian `CustomerProfile.PartyId` directamente sin escanear el Party (Odoo no lo permite limpio; Frappe duplica en 2 DocTypes).

---

## 2. Modelo de entidades

```
Party (agregado raíz — identidad pura)
  ├── ParentPartyId? ──► Party (jerarquía: matriz→sucursal→contacto; delegación campos comerciales)
  ├── FiscalData (owned VO)
  │     ├── RegimenTributario {Ordinario|Simple|Especial}   ◄ un solo eje (DIAN)
  │     ├── ResponsabilidadIVA {Responsable|NoResponsable}  ◄ eje independiente
  │     ├── GranContribuyente, Autorretenedor, AgenteRetencionIVA/ICA
  │     ├── ObligadoLlevarContabilidad, FlagPEP
  ├── CiiuActivities[] ── tabla hija (1..N, una IsPrincipal)
  ├── Addresses[] ─────── MunicipioId DANE
  ├── Phones[], SocialNetworks[]
  ├── Documents[] ─────── compliance con vencimiento
  ├── BankAccounts[] ──── propósito (Pagos/Cobros/Nomina)
  ├── Tags[], Holds[]
  ├── CustomFields (JSONB)
  ├── LegalRepresentative? (owned VO)
  │
  ├── CustomerProfile?  ── faceta cliente
  ├── SupplierProfile?  ── faceta proveedor
  ├── ContactProfile?   ── faceta contacto B2B
  ├── PartnerProfile?   ── faceta socio
  └── EmployeeProfile?  ── faceta empleado (mínimo M1)

CreditAccount (FK a CustomerProfile) + CreditMovement[] (historial inmutable)
CustomerClassification / SupplierClassification (catálogos por tenant)
```

---

## 3. `Party` — agregado raíz (identidad pura)

| Campo | Tipo | DB | Oblig. | Notas |
|---|---|---|---|---|
| `Id` | Guid | uuid PK | Sí | |
| `TenantId` | string | varchar(64) | Sí | Multi-tenant (≠ jerarquía) |
| `ParentPartyId` | Guid? | uuid FK | No | Matriz comercial (Odoo parent_id) |
| `TipoIdentificacion` | enum | smallint | Sí | |
| `NumeroIdentificacion` | string | varchar(20) | Sí | |
| `DigitoVerificacion` | string? | varchar(1) | No | Calculado (algoritmo DIAN) |
| `NombreRazonSocial` | string | varchar(250) | Sí | |
| `NombreComercial` | string? | varchar(250) | No | |
| `TipoPersona` | enum | smallint | Sí | Natural\|Juridica |
| `Email` | string? | varchar(255) | No | |
| `EmailFacturacion` | string? | varchar(255) | No | Para factura electrónica DIAN |
| `Celular`, `WhatsApp`, `WhatsAppName`, `PaginaWeb` | string? | | No | |
| `Genero`, `FechaNacimiento`, `EstadoCivil` | | | No | |
| `Observacion` | string? | text | No | |
| `Estado` | enum | smallint | Sí | Activo\|Inactivo |
| `FiscalData` | owned VO | inline | No | §4 |
| `LegalRepresentative` | owned VO? | inline | No | §5 |
| `CustomFields` | Dictionary | jsonb | No | |

**Delegación al padre (Odoo commercial_fields):** si `ParentPartyId` existe, `FiscalData` y `CreditAccount` pueden resolverse desde la matriz cuando están vacíos en el hijo. Método `ResolveCommercialEntity()` sube por la jerarquía.

**Índices:** único (TenantId, TipoId, NumeroId); GIN full-text español; (TenantId, ParentPartyId); (TenantId, Estado).

---

## 4. `FiscalData` — owned VO (ejes separados)

| Campo | Tipo | Notas |
|---|---|---|
| `RegimenTributario` | enum? | **Ordinario\|Simple\|Especial** — UN régimen (norma DIAN 2026) |
| `ResponsabilidadIVA` | enum? | **Responsable\|NoResponsable** — eje independiente |
| `ResponsabilidadesFiscales` | string[] | Códigos DIAN multi |
| `FormaJuridica` | string? | |
| `GranContribuyente`, `Autorretenedor`, `AgenteRetencionIVA`, `AgenteRetencionICA` | bool | |
| `ObligadoLlevarContabilidad`, `FlagPEP` | bool | |

CIIU NO está aquí — es tabla hija (§7).

---

## 5. `LegalRepresentative` — owned VO
Nombres, Apellidos, TipoId/NumeroId, Telefono(+ext), Celular, Email, EsPEP.

---

## 6. Facetas (Profiles)

### 6.1 `CustomerProfile`
Solo config de faceta. Crédito → `CreditAccount`. Clasificación → catálogo.

| Campo | Notas |
|---|---|
| `PartyId` PK/FK | |
| `ClassificationId?` → CustomerClassification | Mayorista/Minorista (catálogo) |
| `PriceListId?` | |
| `DefaultSalespersonId?`, `DefaultBranchId?` | |
| `WithholdingRuleId?` | Retención (puede diferir del fiscal default) |
| `MaxDiscountPct?`, `AllowDiscount` | |
| `MarketingSourceId?` | |
| `ActivatedOn`, `IsActive` | |

### 6.2 `SupplierProfile`
| Campo | Notas |
|---|---|
| `PartyId` PK/FK | |
| `ClassificationId?` → SupplierClassification | |
| `DefaultCurrencyId` | COP/USD |
| `PaymentTerms` (owned VO) | DiasCredito, FormaPagoId |
| `WithholdingRuleId?`, `DefaultPriceListId?` | |
| `IsDropshipping`, `LeadTimeDays?` | |
| `DefaultBankAccountId?` | Pagos (Frappe) |
| `ActivatedOn`, `IsActive` | |

### 6.3 `ContactProfile`
Empresa madre = `Party.ParentPartyId` (R4), no campo del perfil.
JobTitle, ContactFunction (enum), IsCommercialContact, IsPrimary, ResponsibleUserId.

### 6.4 `PartnerProfile`
SharePercentage, StartDate, EndDate?, Status.

### 6.5 `EmployeeProfile` (mínimo M1)
Contrato/salario/seg.social → Payroll M2.
EmployeeCode, JobTitle, BranchId, CostCenterId, ManagerPartyId, HireDate, IsSalesperson, IsCollector, IsActive.

---

## 7. `PartyCiiuActivity` — CIIU múltiple
PartyId FK, CiiuCode (4 díg), IsPrincipal. Exactamente una principal por Party.

---

## 8. `PartyAddress` (DANE)
PaisId, DepartamentoId, **MunicipioId** (DANE, no "ciudad"), AddressLine1/2, PostalCode, Reference, Lat/Lng, IsPrimary, AddressType.

> Vs Frappe (direcciones compartidas con link polimórfico): Maka las modela como hijas del Party en M1 (más simple). Reconsiderar M2 si aparece bodega compartida en dropshipping.

---

## 9. `CreditAccount` + historial

### `CreditAccount`
| Campo | Notas |
|---|---|
| `Id` | |
| `CustomerProfileId` FK | Crédito es de la faceta cliente |
| `TenantId` | Por compañía (Frappe) |
| `CupoAsignado`, `MonedaId` | |
| `DiasCredito`, `FormaPagoId?` | |
| `EstaActivo`, `FechaAprobacion?`, `AprobadoPor?` | |

Cupo disponible = CupoAsignado − Σ(facturas pendientes). Facturas vienen de Contabilidad M2 vía evento; en M1 se rastrea por movimientos.

### `CreditMovement` (inmutable)
Tipo (AsignacionInicial/Aumento/Reduccion/Consumo/Liberacion/Bloqueo), Monto, SaldoResultante, DocumentoReferenciaId?, Motivo, FechaUtc, RegistradoPor.

---

## 10. `PartyHold` (Frappe Block/Hold)
Reemplaza `PermitirVenta` bool.
HoldType (Ventas/Compras/Pagos/Cobros/Todo), Motivo, FechaInicio, FechaLiberacion? (null=indefinido), EstaActivo, CreadoPor.

Regla: al crear OV/OC/Pago, verificar hold activo del tipo correspondiente. Si existe y vigente, bloquear con motivo.

---

## 11. `PartyBankAccount`
BancoId, TipoCuenta (Ahorros/Corriente), NumeroCuenta, EsPrincipal, Proposito (Pagos/Cobros/Nomina).

---

## 12. Catálogos
`CustomerClassification` / `SupplierClassification`: TenantId, Nombre, Descripcion, Orden, EstaActivo. Por tenant (TecnoImportaciones: Distribuidor/Profesional/Aficionado/Gobierno).

---

## 13. Enums

```csharp
public enum TipoIdentificacion : short { CC=1, CE=2, NIT=3, TI=4, Pasaporte=5, NUIP=6, NITExtranjero=7, RUT=8, PEP=9 }
public enum TipoPersona : short { Natural=1, Juridica=2 }
public enum EstadoParty : short { Inactivo=0, Activo=1 }
// Ejes fiscales SEPARADOS (R4)
public enum RegimenTributario : short { Ordinario=1, Simple=2, Especial=3 }
public enum ResponsabilidadIVA : short { Responsable=1, NoResponsable=2 }
public enum ContactFunction : short { FacturacionElectronica=1, CuentasPorPagar=2, Financiero=3, Comercial=4, TI=5, Gerencia=6, Otro=99 }
public enum HoldType : short { Ventas=1, Compras=2, Pagos=3, Cobros=4, Todo=99 }
public enum CreditMovementType : short { AsignacionInicial=1, Aumento=2, Reduccion=3, Consumo=4, Liberacion=5, Bloqueo=6 }
public enum AddressType : short { Facturacion=1, Envio=2, Fiscal=3, Otra=99 }
public enum TipoCuenta : short { Ahorros=1, Corriente=2 }
public enum BankAccountPurpose : short { Pagos=1, Cobros=2, Nomina=3 }
```

---

## 14. Capacidades

- **CAP-01 CRUD** — identidad única, DV calculado, evento.
- **CAP-02 Jerarquía** (NUEVO) — ParentPartyId sin ciclos, delegación de campos comerciales.
- **CAP-03 Facetas** — N profiles simultáneos, soft deactivate.
- **CAP-04 Crédito** — CreditAccount + movimientos inmutables, cupo disponible derivado.
- **CAP-05 Holds** — bloqueo por tipo + vigencia, valida en OV/OC/Pago.
- **CAP-06 CIIU múltiple** — 1..N, una principal.
- **CAP-07 Direcciones DANE** — MunicipioId validado, una principal.
- **CAP-08 Documentos compliance** — vencimiento + alerta 30 días.
- **CAP-09 Cuentas bancarias** — una principal por propósito.
- **CAP-10 Campos personalizados JSONB.**
- **CAP-11 Etiquetas transversales.**
- **CAP-12 Búsqueda y deduplicación.**

---

## 15. Esquema BD (schema `parties`)

Tablas: `Parties` (con FiscalData + LegalRep owned inline, ParentPartyId self-FK), `CustomerProfiles`, `SupplierProfiles`, `ContactProfiles`, `PartnerProfiles`, `EmployeeProfiles`, `CreditAccounts`, `CreditMovements`, `PartyHolds`, `PartyCiiuActivities` (índice único parcial para principal), `PartyAddresses`, `PartyPhones`, `PartySocialNetworks`, `PartyDocuments`, `PartyBankAccounts`, `PartyTags`, `CustomerClassifications`, `SupplierClassifications`.

Índices clave:
- `ix_parties_identity` único (TenantId, TipoId, NumeroId)
- `ix_parties_fts` GIN full-text español
- `ix_parties_parent` (TenantId, ParentPartyId)
- `ix_ciiu_principal` único parcial WHERE IsPrincipal=true
- `ix_holds_active` parcial WHERE EstaActivo=true
- `ix_creditmov_account` (CreditAccountId, FechaUtc)

(SQL completo en el archivo de trabajo local del SPEC.)

---

## 16. Eventos

**Dominio:** PartyCreated, PartyUpdated, PartyDeactivated, PartyParentAssigned, ProfileActivated/Deactivated, CreditAssigned, CreditMovementRecorded, HoldPlaced/Released, DocumentUploaded.

**Integración (Wolverine):**
| Evento | Consumidores |
|---|---|
| `PartyCreatedIntegrationEvent` | CRM, Mensajería |
| `CustomerProfileActivatedIntegrationEvent` | CRM (oportunidad inicial) |
| `SupplierProfileActivatedIntegrationEvent` | Compras |
| `HoldPlacedIntegrationEvent` | Ventas/Compras, Notificaciones |
| `CreditExhaustedIntegrationEvent` | Ventas (alerta), Notificaciones |

---

## 17. Permisos
View, Create, Update, Delete, Export, ManageProfiles, **ManageCredit**, **ManageHolds**, ApproveDocs.

---

## 18. Divergencias conscientes vs referentes

| Decisión | Odoo | Frappe | Maka | Razón |
|---|---|---|---|---|
| Estructura | res.partner + ranks | Customer/Supplier DocTypes + Link | Party + Profiles (tablas FK) | FK directa sin escanear; sin duplicar identidad |
| Crédito | credit_limit delegado | credit_limits tabla/company | CreditAccount + movimientos | Auditoría DIAN + cupo derivado |
| Jerarquía | parent_id | Party Link | ParentPartyId + delegación | Adopta Odoo |
| Bloqueo | — | Hold tipo+fecha | PartyHold tipo+vigencia | Adopta Frappe |
| Clasificación | category_id | customer_group Link | Classification catálogo/tenant | Adopta Frappe |
| Régimen fiscal | tax-agnostic | tax_category | RegimenTributario + ResponsabilidadIVA | DIAN; ejes ortogonales |
| Direcciones | hijas | independiente + links | hijas (M1) | Simplicidad M1 |

---

## 19. Tests

**Unit:** DV NIT, identidad duplicada, jerarquía sin ciclos, delegación al padre, una dirección principal, una CIIU principal, hold bloquea operación, movimiento crédito recalcula saldo, faceta idempotente.

**Integration:** Party+CustomerProfile+CreditAccount, full-text, filtro por faceta, hold impide OV, tenant isolation.

---

## 20. Estado

| Sección | Estado |
|---|---|
| Normalización vs v1 | ✅ |
| Contraste Odoo/Frappe (código real) | ✅ |
| Modelo dominio | ✅ |
| CAP-01..12 | ✅ |
| Esquema BD | ✅ (principales) |
| Divergencias documentadas | ✅ |
| Implementación | ⏳ M1 |
