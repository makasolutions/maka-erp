> ⚠️ **SPEC HISTÓRICA v1** — la implementación divergió deliberadamente; el as-built canónico
> está en **CLAUDE.md §16 + código**. Útil como visión/razonamiento, **NO como receta**.
> La relación comercial (proveedor↔comprador) NO se modela aquí: ver el diseño de
> **Convenios/Marketplace** (`.agents/rules/modules/agreements-marketplace.md`).

# PARTIES_MODULE_SPEC.md
# Especificación del Módulo de Terceros (Party Pattern) — Maka Omni-Commerce
# Versión: 1.0 | Mayo 2026
# Leer COMPLETO antes de escribir cualquier código.

---

## INSTRUCCIÓN PARA CLAUDE CODE

Este documento es la especificación del módulo Parties.
Antes de implementar CUALQUIER cosa:
1. Lee este documento completo
2. Lee CATALOG_MODULE_SPEC.md — este módulo depende del catálogo
3. Lee `.agents/rules/database.md`
4. Lee `.agents/rules/api-conventions.md`
5. Plan Mode obligatorio → proponer y esperar aprobación de Juan

---

## 1. PRINCIPIOS DE DISEÑO

### 1.1 El Patrón Party — versión pragmática

En lugar del Party Pattern puro (demasiado abstracto para queries cotidianas),
usamos una versión pragmática con tabla base y tablas concretas especializadas:

```
Party (base abstracta — nunca se usa directa en la UI)
  ├── Organization (empresas de cualquier tipo)
  │     ├── TenantAccount     → el cliente del SaaS (plano global)
  │     └── BusinessPartner   → clientes/proveedores del tenant (plano datos)
  └── Person (personas naturales)
        └── Contact           → vinculado a Organization + roles

PartyRole → roles asignados a un Party (Supplier, Customer, Employee...)
PartyAddress → direcciones/sedes de cualquier Party
PartyContact → teléfonos, emails, redes de cualquier Party
```

### 1.2 Dos planos completamente separados

```
PLANO GLOBAL (Root — sin TenantId):
  → TenantAccount: las empresas que contratan el SaaS
  → SupplierGlobal: proveedores elevados a globales por Maka
  → Geography: países, departamentos, municipios
  → TaxRegime, DocumentType: catálogos fiscales globales

PLANO DATOS (con TenantId — Finbuckle):
  → BusinessPartner: clientes y proveedores de cada tenant
  → Contact: personas vinculadas a esas empresas
  → CreditAccount: configuración de crédito por cliente
  → SalesAssignment: vendedores asignados
  → SupplierAgreement: convenios activos con proveedores globales
```

### 1.3 Modelo de proveedores — elevación por SupplierTier

```
Un tenant puede ser proveedor de otros tenants.
La "elevación" es solo un cambio de SupplierTier — los datos NO migran.

SupplierTier:
  None    → tenant normal, sin catálogo proveedor
  Tenant  → proveedor local, catálogo visible solo para convenios directos
  Global  → aprobado por Maka Root Admin, catálogo visible globalmente
             para tenants con convenio aprobado

Flujo de elevación:
  1. Tenant tiene múltiples clientes que lo tienen como proveedor
  2. Maka Root Admin aprueba elevación → SupplierTier = Global
  3. El catálogo del tenant (OwnerId = TenantId_Sony) queda visible
     globalmente SIN mover datos — solo cambia la visibilidad
  4. Los tenants sin convenio activo NO pueden ver ese catálogo
```

### 1.4 Snapshot en documentos — regla crítica

Cuando se crea una factura, pedido o cualquier documento comercial:
- Los datos del tercero (NIT, razón social, dirección, régimen) se **copian**
  al documento en el momento de creación
- Si el tercero cambia sus datos después, los documentos anteriores mantienen
  los datos originales con los que fueron creados
- Nunca usar FK directa a BusinessPartner en la cabecera del documento —
  usar los campos copiados (snapshot)
- Solo guardar la FK como referencia histórica para navegación

---

## 2. MAESTROS GEOGRÁFICOS (Plano Global)

### 2.1 Country

```csharp
public class Country : BaseEntity
{
    public string Name        { get; private set; }  // "Colombia", "Estados Unidos"
    public string IsoCode2    { get; private set; }  // "CO", "US"
    public string IsoCode3    { get; private set; }  // "COL", "USA"
    public string PhoneCode   { get; private set; }  // "+57", "+1"
    public string CurrencyCode{ get; private set; }  // "COP", "USD"
    public bool   IsActive    { get; private set; }

    public ICollection<Department> Departments { get; private set; }
}
```

### 2.2 Department (Departamento / Estado / Provincia)

```csharp
public class Department : BaseEntity
{
    public Guid   CountryId   { get; private set; }
    public string Name        { get; private set; }  // "Cundinamarca", "Antioquia"
    public string Code        { get; private set; }  // "CUN", "ANT" — código DANE
    public bool   IsActive    { get; private set; }

    public ICollection<Municipality> Municipalities { get; private set; }
}
```

### 2.3 Municipality (Municipio / Ciudad)

```csharp
public class Municipality : BaseEntity
{
    public Guid   DepartmentId { get; private set; }
    public string Name         { get; private set; }  // "Bogotá D.C.", "Medellín"
    public string DaneCode     { get; private set; }  // código DANE Colombia: "11001", "05001"
    public bool   IsActive     { get; private set; }
}
```

**Seed inicial:** Colombia completa (32 departamentos + 1122 municipios desde el DANE).
Países principales: Colombia, Estados Unidos, China, Alemania, Japón, Australia, Francia, España.

---

## 3. CATÁLOGOS FISCALES (Plano Global)

### 3.1 DocumentType (Tipos de documento de identidad)

```csharp
public class DocumentType : BaseEntity
{
    public string Code        { get; private set; }  // "NIT", "CC", "CE", "PAS", "RUT", "TI"
    public string Name        { get; private set; }  // "NIT", "Cédula de Ciudadanía"...
    public string Description { get; private set; }
    public bool   RequiresVerificationDigit { get; private set; }  // true solo para NIT
    public bool   IsForOrganization         { get; private set; }  // NIT, RUT
    public bool   IsForPerson               { get; private set; }  // CC, CE, PAS, TI
    public bool   IsActive    { get; private set; }
}
```

**Seed:** NIT, CC, CE, Pasaporte, RUT, TI, NIUP, NIT extranjero

### 3.2 TaxRegime (Régimen tributario — Colombia)

```csharp
public class TaxRegime : BaseEntity
{
    public string Code        { get; private set; }
    public string Name        { get; private set; }
    public string DianCode    { get; private set; }  // código DIAN para facturación electrónica
    public bool   IsActive    { get; private set; }
}
```

**Seed completo Colombia:**

| Code | Name | DianCode |
|---|---|---|
| `IVA_RESP` | Responsable de IVA | `48` |
| `IVA_NO_RESP` | No Responsable de IVA | `49` |
| `GRAN_CONTRIB` | Gran Contribuyente | `GC` |
| `SIMPLE` | Régimen Simple de Tributación | `RS` |
| `AUTORETENEDOR` | Autorretenedor | `AU` |
| `ESPECIAL` | Régimen Especial | `RE` |
| `ENTIDAD_GOB` | Entidad del Estado | `EG` |
| `NO_APLICA` | No Aplica (Extranjero) | `NA` |

---

## 4. PARTY BASE (Abstracción central)

### 4.1 Party

```csharp
// Tabla base — nunca se instancia directamente en la UI
// Se accede siempre a través de Organization o Person
public abstract class Party : AggregateRoot, ISoftDeletable
{
    public PartyType  Type       { get; private set; }  // Organization | Person
    public string     Name       { get; private set; }  // Razón social o nombre completo
    public bool       IsActive   { get; private set; }
    public bool       IsDeleted  { get; private set; }

    // Relaciones
    public ICollection<PartyRole>    Roles    { get; private set; }
    public ICollection<PartyAddress> Addresses{ get; private set; }
    public ICollection<PartyContact> Contacts { get; private set; }
}

public enum PartyType { Organization, Person }
```

### 4.2 PartyRole (Roles asignados)

```csharp
public class PartyRole : BaseEntity
{
    public Guid       PartyId    { get; private set; }
    public RoleType   Role       { get; private set; }
    public bool       IsActive   { get; private set; }
    public DateTime   AssignedAt { get; private set; }
    public DateTime?  ExpiresAt  { get; private set; }  // null = permanente
    public string?    Notes      { get; private set; }
}

public enum RoleType
{
    // Roles de BusinessPartner (plano datos del tenant)
    Customer,       // cliente que compra
    Supplier,       // proveedor que vende
    Employee,       // empleado (para HR futuro)
    Carrier,        // transportadora
    Agent,          // agente/intermediario
    Competitor,     // competidor (para inteligencia comercial)

    // Roles de TenantAccount (plano global)
    TenantOwner,    // dueño del tenant del SaaS
    GlobalSupplier, // proveedor elevado a global por Maka
}
```

### 4.3 PartyAddress (Sedes y direcciones)

```csharp
public class PartyAddress : BaseEntity
{
    public Guid    PartyId        { get; private set; }

    // Nombre y tipo de sede
    public string  AddressName    { get; private set; }   // "Oficina Principal", "Bodega Norte"
    public AddressType Type       { get; private set; }
    public bool    IsMain         { get; private set; }   // sede/dirección principal
    public bool    IsWarehouse    { get; private set; }   // es bodega de inventario
    public bool    IsBilling      { get; private set; }   // dirección de facturación
    public bool    IsShipping     { get; private set; }   // dirección de envío

    // Datos geográficos normalizados
    public Guid    CountryId      { get; private set; }
    public Guid    DepartmentId   { get; private set; }
    public Guid    MunicipalityId { get; private set; }
    public string  AddressLine1   { get; private set; }   // "Calle 93 # 11-27"
    public string? AddressLine2   { get; private set; }   // "Piso 3, Oficina 302"
    public string? PostalCode     { get; private set; }   // "110221"
    public string? Neighborhood   { get; private set; }   // "El Chicó"
    public decimal? Latitude      { get; private set; }   // para geolocalización futura
    public decimal? Longitude     { get; private set; }

    // Contacto de la sede
    public string? Phone          { get; private set; }
    public string? Email          { get; private set; }
    public string? ContactPerson  { get; private set; }   // persona de contacto en esa sede
}

public enum AddressType
{
    Main,       // sede principal
    Branch,     // sucursal
    Warehouse,  // bodega
    Billing,    // facturación
    Shipping,   // envío/despacho
    Other
}
```

### 4.4 PartyContact (Teléfonos, emails, redes)

```csharp
public class PartyContact : BaseEntity
{
    public Guid         PartyId     { get; private set; }
    public ContactType  Type        { get; private set; }
    public string       Value       { get; private set; }   // el número, email o URL
    public string?      Label       { get; private set; }   // "WhatsApp", "Facturación", "Soporte"
    public bool         IsPrimary   { get; private set; }
    public bool         IsActive    { get; private set; }
}

public enum ContactType
{
    Phone, Mobile, WhatsApp, Email,
    Website, LinkedIn, Instagram, Facebook,
    Fax, Other
}
```

---

## 5. ORGANIZATION (Empresas)

```csharp
public class Organization : Party
{
    // Identificación fiscal
    public Guid    DocumentTypeId     { get; private set; }  // NIT, RUT, etc.
    public string  DocumentNumber     { get; private set; }  // "900744809"
    public string? VerificationDigit  { get; private set; }  // "4" (solo para NIT)
    public string  LegalName          { get; private set; }  // "MAKA SOLUTIONS SAS"
    public string? TradeName          { get; private set; }  // "TECNOIMPORTACIONES.COM"
    public string? ShortName          { get; private set; }  // "TECNOIMPORTACIONES"

    // Clasificación fiscal
    public Guid    TaxRegimeId        { get; private set; }
    public string? CiiuCode           { get; private set; }  // código actividad económica CIIU
    public string? CiiuDescription    { get; private set; }
    public LegalType LegalType        { get; private set; }
    //  SAS, LTDA, SA, EU, SRL, NaturalPerson, Other

    // Información comercial
    public string? Website            { get; private set; }
    public string? LogoUrl            { get; private set; }
    public string? Description        { get; private set; }

    // Para proveedores: tier de visibilidad
    public SupplierTier SupplierTier  { get; private set; }  // None | Tenant | Global
}

public enum LegalType { SAS, LTDA, SA, EU, SRL, NaturalPerson, Cooperative, Foundation, Other }
public enum SupplierTier { None, Tenant, Global }
```

---

## 6. PERSON (Personas naturales)

```csharp
public class Person : Party
{
    // Identificación
    public Guid    DocumentTypeId  { get; private set; }  // CC, CE, PAS, TI
    public string  DocumentNumber  { get; private set; }
    public string  FirstName       { get; private set; }
    public string? MiddleName      { get; private set; }
    public string  LastName        { get; private set; }
    public string? SecondLastName  { get; private set; }
    // Name heredado de Party = FirstName + LastName (calculado)

    // Datos personales
    public DateTime? BirthDate     { get; private set; }
    public string?   Gender        { get; private set; }  // "M" | "F" | "NB" | null
    public string?   Nationality   { get; private set; }  // código ISO país

    // Clasificación fiscal (una persona natural puede ser Responsable de IVA)
    public Guid?   TaxRegimeId     { get; private set; }

    // Relaciones con organizaciones
    public ICollection<OrganizationPerson> Organizations { get; private set; }
}
```

### 6.1 OrganizationPerson (Vinculación persona ↔ empresa)

```csharp
public class OrganizationPerson : BaseEntity
{
    public Guid    OrganizationId  { get; private set; }
    public Guid    PersonId        { get; private set; }
    public string  JobTitle        { get; private set; }   // "Gerente de Compras"
    public string? Department      { get; private set; }   // "Compras", "Contabilidad"
    public bool    IsPrimaryContact{ get; private set; }   // contacto principal de la empresa
    public bool    IsActive        { get; private set; }
    public DateTime StartDate      { get; private set; }
    public DateTime? EndDate       { get; private set; }

    // Permisos específicos de este contacto en la relación
    public bool    CanApproveOrders      { get; private set; }
    public bool    CanReceiveInvoices    { get; private set; }
    public bool    CanNegotiatePrices    { get; private set; }
}
```

---

## 7. TENANT ACCOUNT (Plano Global — cliente del SaaS)

```csharp
// Extiende Organization para el cliente del SaaS
// Vive en el plano global (TenantId = null)
// Se conecta con Finbuckle MultiTenant via TenantIdentifier

public class TenantAccount : Organization
{
    // Vínculo con Finbuckle
    public string  TenantIdentifier    { get; private set; }  // el Id de Finbuckle
    public string  TenantName          { get; private set; }  // nombre del tenant en Finbuckle

    // Suscripción SaaS
    public SubscriptionPlan Plan       { get; private set; }
    public DateTime   PlanStartDate    { get; private set; }
    public DateTime?  PlanEndDate      { get; private set; }
    public bool       IsTrialPeriod    { get; private set; }
    public DateTime?  TrialEndsAt      { get; private set; }

    // Quotas del plan
    public int    MaxUsers             { get; private set; }
    public int    MaxWarehouses        { get; private set; }
    public int    MaxProducts          { get; private set; }  // -1 = ilimitado
    public int    MaxMonthlyInvoices   { get; private set; }  // -1 = ilimitado

    // Configuración del sistema (ver §8)
    public TenantSettings Settings     { get; private set; }
}

public enum SubscriptionPlan { Trial, Starter, Business, Enterprise, Custom }
```

---

## 8. TENANT SETTINGS (Configuración del sistema por tenant)

```csharp
// Configuración parametrizable del tenant
// Accesible desde Settings → Empresa en el dashboard

public class TenantSettings : BaseEntity
{
    public Guid   TenantAccountId         { get; private set; }

    // ── FISCAL ────────────────────────────────────────────────────
    public Guid   DefaultTaxRegimeId      { get; private set; }  // régimen del tenant
    public string DefaultCurrency         { get; private set; }  // "COP"
    public bool   PricesIncludeTax        { get; private set; }  // precios con IVA incluido
    public bool   AutoCalculateRetention  { get; private set; }  // retención automática

    // ── FACTURACIÓN DIAN ──────────────────────────────────────────
    public string? DianResolutionNumber   { get; private set; }
    public string? DianResolutionPrefix   { get; private set; }
    public long?   DianResolutionFrom     { get; private set; }
    public long?   DianResolutionTo       { get; private set; }
    public DateTime? DianResolutionDate   { get; private set; }
    public DateTime? DianResolutionExpiry { get; private set; }
    public string?  PtaProvider           { get; private set; }  // "Alegra", "Siigo", etc.
    public string?  PtaApiKey             { get; private set; }  // encriptado
    public bool     DianSandboxMode       { get; private set; }

    // ── VENTAS ────────────────────────────────────────────────────
    public int    DefaultPaymentTermDays  { get; private set; }  // plazo default: 30 días
    public bool   RequireApprovalForOrders{ get; private set; }
    public decimal MinOrderAmount         { get; private set; }
    public bool   AllowPartialPayments    { get; private set; }
    public bool   AllowBackorders         { get; private set; }

    // ── INVENTARIO ────────────────────────────────────────────────
    public StockValuationMethod ValuationMethod { get; private set; }
    //  WeightedAverage | FIFO | LIFO
    public bool   AllowNegativeStock      { get; private set; }
    public bool   RequirePickingList      { get; private set; }

    // ── NOTIFICACIONES ────────────────────────────────────────────
    public bool   NotifyLowStock          { get; private set; }
    public bool   NotifyNewOrder          { get; private set; }
    public bool   NotifyPaymentReceived   { get; private set; }
    public string? NotificationEmail      { get; private set; }

    // ── DOCUMENTOS ────────────────────────────────────────────────
    public string? InvoiceHeaderText      { get; private set; }  // texto personalizado en facturas
    public string? InvoiceFooterText      { get; private set; }
    public string? QuoteTermsText         { get; private set; }  // términos en cotizaciones
    public string? OrderConfirmationText  { get; private set; }
}

public enum StockValuationMethod { WeightedAverage, FIFO, LIFO }
```

---

## 9. BUSINESS PARTNER (Terceros del tenant — clientes y proveedores)

```csharp
// Plano datos: TenantId de Finbuckle
// Puede ser tanto Organization como Person
// Un BusinessPartner puede tener múltiples roles (Customer + Supplier al mismo tiempo)

public class BusinessPartner : Organization  // o Person — usar TPH (Table Per Hierarchy)
{
    // Clasificación comercial
    public PartnerCategory Category    { get; private set; }
    //  Individual, SmallBusiness, MediumBusiness, Enterprise, Government, Nonprofit

    // Vendedor asignado (ver §10 para asignación flexible)
    public Guid?   DefaultSalesPersonId { get; private set; }  // vendedor general

    // Lista de precios default
    public Guid?   DefaultPriceListId   { get; private set; }  // lista de precios asignada
    public string  PriceSegment         { get; private set; }  // "retail" | "wholesale" | "vip" | "b2b"
                                                                // Este valor va al JWT del cliente

    // Crédito (ver §11)
    public bool    HasCredit            { get; private set; }
    public CreditAccount? Credit        { get; private set; }

    // Facturación
    public Guid?   BillingAddressId     { get; private set; }
    public Guid?   ShippingAddressId    { get; private set; }
    public string? InternalCode         { get; private set; }  // código interno del tenant
    public string? Notes                { get; private set; }

    // Relación con proveedor global (si el BusinessPartner es proveedor)
    public SupplierTier SupplierTier    { get; private set; }
    public ICollection<SupplierAgreement> Agreements { get; private set; }
}

public enum PartnerCategory
{
    Individual, SmallBusiness, MediumBusiness,
    Enterprise, Government, Nonprofit, Other
}
```

---

## 10. SALES ASSIGNMENT (Vendedores asignados — modelo flexible)

```csharp
// Modelo de 3 niveles: General → Marca → Categoría
// Nivel más específico tiene prioridad sobre el más general

public class SalesAssignment : BaseEntity
{
    public Guid   BusinessPartnerId  { get; private set; }
    public Guid   SalesPersonId      { get; private set; }  // FK a Identity User

    // Granularidad de la asignación
    public AssignmentScope Scope     { get; private set; }
    public Guid?  BrandId            { get; private set; }  // null si Scope != Brand
    public Guid?  CategoryId         { get; private set; }  // null si Scope != Category

    public bool   IsActive           { get; private set; }
    public DateTime AssignedAt       { get; private set; }
    public DateTime? ExpiresAt       { get; private set; }
    public string? Notes             { get; private set; }
}

public enum AssignmentScope
{
    General,    // aplica a todos los productos del cliente
    Brand,      // aplica solo cuando el pedido es de esta marca
    Category    // aplica solo cuando el pedido es de esta categoría
}
```

**Lógica de resolución del vendedor (en el handler de pedidos):**

```csharp
// Prioridad: Category > Brand > General
private Guid? ResolveSellerForLineItem(Guid partnerId, Guid? brandId, Guid? categoryId)
{
    // 1. Buscar asignación por categoría específica
    if (categoryId.HasValue)
    {
        var byCat = assignments
            .Where(a => a.Scope == AssignmentScope.Category
                     && a.CategoryId == categoryId
                     && a.IsActive).FirstOrDefault();
        if (byCat != null) return byCat.SalesPersonId;
    }

    // 2. Buscar asignación por marca
    if (brandId.HasValue)
    {
        var byBrand = assignments
            .Where(a => a.Scope == AssignmentScope.Brand
                     && a.BrandId == brandId
                     && a.IsActive).FirstOrDefault();
        if (byBrand != null) return byBrand.SalesPersonId;
    }

    // 3. Fallback: vendedor general del cliente
    return partner.DefaultSalesPersonId;
}
```

---

## 11. CREDIT ACCOUNT (Crédito por cliente)

```csharp
public class CreditAccount : BaseEntity
{
    public Guid    BusinessPartnerId  { get; private set; }

    // Parámetros del crédito
    public decimal CreditLimit        { get; private set; }   // cupo aprobado en COP
    public int     PaymentTermDays    { get; private set; }   // 30 | 60 | 90 días
    public int     MaxOverdueDays     { get; private set; }   // días mora antes de bloqueo
    public decimal MinimumPayment     { get; private set; }   // pago mínimo para desbloquear

    // Estado
    public CreditStatus Status        { get; private set; }
    public BlockReason? BlockReason   { get; private set; }
    public string?  BlockNotes        { get; private set; }
    public DateTime? BlockedAt        { get; private set; }
    public string?  BlockedByUserId   { get; private set; }
    public DateTime? ApprovedAt       { get; private set; }
    public string?  ApprovedByUserId  { get; private set; }

    // Campos calculados en módulo Cartera (Fase siguiente)
    // CurrentBalance    → suma de facturas pendientes de pago
    // AvailableCredit   → CreditLimit - CurrentBalance
    // OldestInvoiceDate → fecha de la factura más antigua sin pagar
    // DaysOverdue       → días desde OldestInvoiceDate
    // Estos campos NO están en esta tabla — se calculan en tiempo real
    // cuando se implemente el módulo de Cartera
}

public enum CreditStatus { Active, Blocked, Suspended, PendingApproval }
public enum BlockReason  { ManualBlock, OverLimit, OverdueDays, LegalIssue }
```

### 11.1 CreditAccountHistory (Historial de cambios al crédito)

```csharp
public class CreditAccountHistory : BaseEntity
{
    public Guid    CreditAccountId    { get; private set; }
    public string  FieldChanged       { get; private set; }   // "CreditLimit", "Status", "PaymentTermDays"
    public string  OldValue           { get; private set; }
    public string  NewValue           { get; private set; }
    public string  ChangedByUserId    { get; private set; }
    public string? ChangeReason       { get; private set; }
    // CreatedAt heredado de BaseEntity — INMUTABLE
}
```

---

## 12. SUPPLIER AGREEMENT (Convenios con proveedores)

```csharp
// Un tenant formaliza un convenio con un proveedor (global o tenant)
// Sin convenio activo: el tenant NO puede ver el catálogo del proveedor

public class SupplierAgreement : BaseEntity
{
    public Guid   TenantPartnerId         { get; private set; }  // el tenant que solicita
    public Guid   SupplierPartnerId       { get; private set; }  // el proveedor

    // Estado del convenio
    public AgreementStatus Status         { get; private set; }
    public DateTime   RequestedAt         { get; private set; }
    public DateTime?  ApprovedAt          { get; private set; }
    public string?    ApprovedByUserId    { get; private set; }   // usuario del proveedor
    public DateTime?  ExpiresAt           { get; private set; }   // null = permanente
    public DateTime?  TerminatedAt        { get; private set; }
    public string?    TerminationReason   { get; private set; }

    // Condiciones del convenio
    public decimal?   DiscountPercent     { get; private set; }   // % descuento global
    public decimal?   MinOrderAmount      { get; private set; }   // pedido mínimo
    public int?       PaymentTermDays     { get; private set; }   // plazo de pago acordado
    public string?    Conditions          { get; private set; }   // texto libre de condiciones

    // Lista de precios que se habilita al aprobar
    public Guid?      PriceListId         { get; private set; }   // lista mayorista del proveedor

    // Documentación del proceso
    public ICollection<AgreementDocument> Documents { get; private set; }
}

public enum AgreementStatus
{
    Requested,    // tenant envió solicitud
    UnderReview,  // proveedor está revisando
    Approved,     // activo
    Rejected,     // rechazado por el proveedor
    Expired,      // venció la fecha
    Terminated    // terminado por cualquiera de las partes
}
```

### 12.1 AgreementDocument (Documentación del convenio)

```csharp
public class AgreementDocument : BaseEntity
{
    public Guid    AgreementId    { get; private set; }
    public string  DocumentName   { get; private set; }   // "RUT", "Cámara de Comercio"
    public string  FileUrl        { get; private set; }
    public string  UploadedBy     { get; private set; }
    public bool    IsApproved     { get; private set; }
    public string? ReviewNotes    { get; private set; }
}
```

---

## 13. PLAN DE MIGRACIONES

```
Migración 1: AddGeography
  → Country, Department, Municipality
  + Seed: Colombia completa + países principales

Migración 2: AddFiscalCatalogs
  → DocumentType, TaxRegime
  + Seed: todos los tipos DIAN

Migración 3: AddPartyBase
  → Party, PartyRole, PartyAddress, PartyContact
  → Organization, Person
  → OrganizationPerson

Migración 4: AddTenantAccount
  → TenantAccount, TenantSettings

Migración 5: AddBusinessPartner
  → BusinessPartner
  → SalesAssignment
  → CreditAccount, CreditAccountHistory

Migración 6: AddSupplierAgreements
  → SupplierAgreement, AgreementDocument
```

---

## 14. FEATURES BACKEND — VERTICAL SLICE

### 14.1 Geography (solo lectura — datos del seed)

| Feature | Tipo | Notas |
|---|---|---|
| `GetCountries` | Query | Lista activa de países |
| `GetDepartments` | Query | Filtrado por CountryId |
| `GetMunicipalities` | Query | Filtrado por DepartmentId, búsqueda por nombre o código DANE |

### 14.2 TenantAccount & Settings

| Feature | Tipo | Notas |
|---|---|---|
| `GetTenantAccount` | Query | Datos de la empresa del tenant actual |
| `UpdateTenantAccount` | Command | Actualizar razón social, NIT, logo, etc. |
| `GetTenantSettings` | Query | Configuración del sistema |
| `UpdateTenantSettings` | Command | Actualizar parámetros del sistema |
| `ElevateTenantToGlobalSupplier` | Command | Solo Root Admin — cambia SupplierTier = Global |

### 14.3 BusinessPartner

| Feature | Tipo | Notas |
|---|---|---|
| `CreateBusinessPartner` | Command | Crea Organization o Person con roles |
| `UpdateBusinessPartner` | Command | Actualiza datos generales |
| `DeleteBusinessPartner` | Command | Soft delete — verificar sin documentos activos |
| `GetBusinessPartnerById` | Query | Detalle completo con roles, direcciones, crédito |
| `GetBusinessPartners` | Query | Lista paginada con filtros: rol, categoría, estado |
| `AssignRole` | Command | Agregar rol a un partner (Customer, Supplier...) |
| `RemoveRole` | Command | Quitar rol |

### 14.4 Addresses

| Feature | Tipo | Notas |
|---|---|---|
| `AddAddress` | Command | Agregar sede/dirección a un Party |
| `UpdateAddress` | Command | Actualizar dirección |
| `DeleteAddress` | Command | No eliminar si es la única o si es sede principal |
| `SetMainAddress` | Command | Marcar como sede principal |

### 14.5 Credit

| Feature | Tipo | Notas |
|---|---|---|
| `CreateCreditAccount` | Command | Crear cuenta de crédito para un cliente |
| `UpdateCreditLimit` | Command | Cambiar cupo + registrar en History |
| `BlockCredit` | Command | Bloquear crédito con razón |
| `UnblockCredit` | Command | Desbloquear (requiere permiso especial) |
| `GetCreditHistory` | Query | Historial de cambios del crédito |

### 14.6 Sales Assignment

| Feature | Tipo | Notas |
|---|---|---|
| `AssignSalesPerson` | Command | Asignar vendedor (General, Brand, Category) |
| `RemoveSalesAssignment` | Command | Quitar asignación |
| `GetSalesAssignments` | Query | Asignaciones de un BusinessPartner |
| `ResolveSalesPerson` | Query | Dado un partner + brand + category, ¿qué vendedor aplica? |

### 14.7 Supplier Agreements

| Feature | Tipo | Notas |
|---|---|---|
| `RequestAgreement` | Command | Tenant solicita convenio con proveedor |
| `UploadAgreementDocument` | Command | Subir documentación |
| `ReviewAgreement` | Command | Proveedor aprueba/rechaza |
| `TerminateAgreement` | Command | Terminar convenio activo |
| `GetAgreements` | Query | Convenios del tenant actual |
| `GetPendingApprovals` | Query | Solicitudes pendientes (vista del proveedor) |

---

## 15. FEATURES FRONTEND

### 15.1 Páginas del dashboard

| Ruta | Descripción |
|---|---|
| `/parties` | Lista general de todos los terceros con filtro por rol |
| `/parties/new` | Crear empresa o persona natural |
| `/parties/:id` | Detalle del tercero — tabs: General, Sedes, Contactos, Crédito, Vendedores, Convenios, Historial |
| `/parties/:id/credit` | Gestión de crédito |
| `/settings/company` | Datos de la empresa (TenantAccount) |
| `/settings/system` | Parámetros del sistema (TenantSettings) |
| `/suppliers` | Directorio de proveedores globales disponibles |
| `/suppliers/:id` | Perfil del proveedor + marcas/categorías + solicitar convenio |
| `/agreements` | Mis convenios activos y pendientes |

### 15.2 Tabs del detalle de tercero (/parties/:id)

```
Tab 1: General
  Tipo (empresa/persona), Nombre/Razón social, Documento,
  Régimen tributario, Categoría, Roles asignados,
  Lista de precios default, Segmento de precio, Notas

Tab 2: Sedes y Direcciones
  MakaGrid con sedes: nombre, tipo, municipio, dirección, principal
  Botón agregar sede → formulario con selección geográfica normalizada
  Marcar sede principal, sede de facturación, sede de envío

Tab 3: Contactos
  Personas vinculadas a la empresa con cargo y permisos
  Teléfonos, emails, WhatsApp
  Agregar contacto → buscar persona existente o crear nueva

Tab 4: Crédito
  Toggle "Tiene crédito"
  Cupo, Plazo días, Días mora permitidos
  Estado actual (Active/Blocked) con razón si aplica
  MakaGrid con historial de cambios

Tab 5: Vendedores
  Vendedor general asignado
  MakaGrid con asignaciones específicas por marca/categoría

Tab 6: Convenios (si es proveedor)
  Lista de convenios activos con este proveedor
  Documentación subida

Tab 7: Historial de cambios
  EntityAuditSection
```

---

## 16. CONEXIONES CON OTROS MÓDULOS

```
CATALOG:
  BusinessPartner.DefaultPriceListId  → PriceList.Id
  BusinessPartner.PriceSegment        → PriceList.CustomerSegment (claim JWT)
  SalesAssignment.BrandId             → Brand.Id
  SalesAssignment.CategoryId          → Category.Id
  SupplierAgreement.PriceListId       → PriceList.Id (lista mayorista del proveedor)

INVENTORY (futuro):
  PartyAddress (IsWarehouse=true)     → Warehouse.AddressId
  BusinessPartner                     → referenciado en StockMovement.ReferenceDocument

ORDERS (futuro):
  BusinessPartner.Id                  → Order.CustomerId (FK de referencia)
  Snapshot del BusinessPartner        → Order.CustomerSnapshot (JSONB — inmutable)
  CreditAccount                       → validado antes de confirmar pedido

BILLING (futuro):
  TenantSettings.DianResolution*      → usado al emitir facturas
  BusinessPartner.TaxRegimeId         → determina retenciones automáticas
  BusinessPartner.DocumentType        → va en el XML de la factura DIAN

IDENTITY:
  SalesAssignment.SalesPersonId       → Identity.User.Id
  CreditAccount.BlockedByUserId       → Identity.User.Id
  AgreementDocument.UploadedBy        → Identity.User.Id
```

---

## 17. REGLAS DE NEGOCIO CRÍTICAS

```
DOCUMENTOS DE IDENTIDAD:
  NIT debe tener dígito de verificación (campo separado, no parte del número).
  Un NIT es único globalmente (no puede haber dos Organization con el mismo NIT).
  Una CC/CE es única por tipo de documento.

SOFT DELETE CON RESTRICCIONES:
  No eliminar BusinessPartner si tiene pedidos, facturas o convenios activos.
  No eliminar una sede si es la única del Party.
  No eliminar una sede si tiene stock asignado en Inventory.

CRÉDITO:
  Si CreditStatus = Blocked → el módulo de Orders debe rechazar nuevos pedidos
  (excepto si el pedido es de contado).
  Solo usuarios con Permissions.Parties.Credit.Unblock pueden desbloquear.
  Cada cambio de CreditLimit o Status genera registro en CreditAccountHistory.

VENDOR ASSIGNMENT:
  Prioridad: Category > Brand > General.
  Si no hay ninguna asignación → el pedido queda sin vendedor asignado.

SUPPLIER AGREEMENTS:
  Sin AgreementStatus = Approved → el tenant NO puede ver el catálogo del proveedor.
  La lista de precios del proveedor solo se activa al aprobar el convenio.
  Al terminar el convenio → el tenant pierde acceso al catálogo del proveedor.

SUPPLIER TIER ELEVATION:
  Solo Root Admin con Permissions.Root.Suppliers.Elevate puede cambiar SupplierTier.
  La elevación NO mueve datos — solo cambia el campo SupplierTier.
  La elevación es reversible (se puede volver a Tenant).

SNAPSHOT EN DOCUMENTOS:
  Al crear Order, Invoice, Quote → copiar en el documento:
    CustomerName, CustomerDocument, CustomerAddress, CustomerTaxRegime
  Estos campos en el documento son INMUTABLES.
  El BusinessPartner.Id se guarda como referencia de navegación únicamente.

GEOGRAPHY:
  Los municipios colombianos usan código DANE.
  Al registrar dirección: País → Departamento → Municipio son campos normalizados.
  AddressLine1 es texto libre para la calle/carrera/número.
```

---

## 18. PERMISOS

```
Permissions.Parties.View
Permissions.Parties.Create
Permissions.Parties.Update
Permissions.Parties.Delete
Permissions.Parties.Credit.View
Permissions.Parties.Credit.Manage
Permissions.Parties.Credit.Unblock        ← requiere aprobación especial
Permissions.Parties.SalesAssignment.Manage
Permissions.Parties.Agreements.Request
Permissions.Parties.Agreements.Approve    ← solo el proveedor
Permissions.Parties.Agreements.Terminate

Permissions.Settings.Company.View
Permissions.Settings.Company.Update
Permissions.Settings.System.View
Permissions.Settings.System.Update
Permissions.Settings.Dian.Manage          ← configuración DIAN sensible

Permissions.Root.Suppliers.Elevate        ← solo Root Admin de Maka
Permissions.Root.TenantAccounts.Manage    ← solo Root Admin de Maka
```

---

## 19. TRADUCCIONES REQUERIDAS

En `public/locales/es/parties.json` y `en/parties.json`:

```json
{
  "title": "Terceros",
  "organization": { "title", "create", "legalName", "tradeName", "nit", "regime" },
  "person": { "title", "create", "firstName", "lastName", "document" },
  "roles": { "customer", "supplier", "employee", "carrier" },
  "address": { "title", "addSede", "main", "warehouse", "billing", "shipping" },
  "credit": { "title", "limit", "terms", "status", "block", "unblock", "history" },
  "creditStatus": { "active", "blocked", "suspended", "pendingApproval" },
  "blockReason": { "manualBlock", "overLimit", "overdueDays", "legalIssue" },
  "agreements": { "title", "request", "approve", "reject", "terminate", "conditions" },
  "agreementStatus": { "requested", "underReview", "approved", "rejected", "expired", "terminated" },
  "salesAssignment": { "title", "assign", "general", "byBrand", "byCategory" },
  "supplierTier": { "none", "tenant", "global" }
}
```

---

## 20. SEED INICIAL

```
DocumentTypes: NIT, CC, CE, Pasaporte, RUT, TI, NIUP, NIT extranjero
TaxRegimes: todos los 8 regímenes colombianos
Countries: Colombia + 7 países principales
Departments: 33 departamentos colombianos (32 + Bogotá D.C.)
Municipalities: 1122 municipios Colombia (fuente: DANE)

TenantAccount inicial (para Tecnoimportaciones):
  LegalName: "MAKA SOLUTIONS SAS"
  TradeName: "TECNOIMPORTACIONES.COM"
  NIT: 900744809-4
  TaxRegime: Responsable de IVA
  Plan: Business
  DefaultCurrency: COP
```

---

## 21. SEÑALES DE ALERTA — PARAR Y PREGUNTAR

```
⚠️  Se intenta crear dos Organization con el mismo NIT
⚠️  Se intenta eliminar el último BusinessPartner con convenio activo
⚠️  Se intenta bloquear crédito sin razón especificada
⚠️  Se intenta elevar SupplierTier sin ser Root Admin
⚠️  Se intenta acceder al catálogo de un proveedor sin convenio aprobado
⚠️  Se modifica TenantSettings.PtaApiKey — debe almacenarse encriptado
⚠️  Se intenta eliminar una sede que es la única del Party
⚠️  Una migración afecta la tabla Party o sus hijos con datos existentes
```

---

*Versión: 1.0 | Módulo: Parties | Mayo 2026*
*Fuente de verdad para el módulo de Terceros.*
*Guardar en: `.agents/rules/modules/parties.md`*
*Depende de: CATALOG_MODULE_SPEC.md (PriceList, Brand, Category)*

---

## 22. DATOS MAESTROS CONTABLES (Atados a la empresa/tenant)

> Estas entidades son maestros de la empresa — deben existir antes que cualquier
> documento contable (facturas, pedidos, asientos). Se crean en este módulo
> porque pertenecen a la configuración del tenant, no a Contabilidad.

### 22.1 Branch (Sucursales del tenant)

```csharp
// Sucursales físicas de la empresa tenant
// Diferente de PartyAddress: la sucursal es una unidad operativa con
// código propio para DIAN, centros de costo y reportes gerenciales

public class Branch : BaseEntity
{
    // Identificación
    public string   Code          { get; private set; }  // "BOG", "MED", "CAL" — código interno
    public string   Name          { get; private set; }  // "Bogotá", "Medellín"
    public string?  Description   { get; private set; }

    // Dirección vinculada al Party del tenant
    public Guid?    AddressId     { get; private set; }  // FK a PartyAddress

    // Datos DIAN (cada sucursal puede tener su propio establecimiento)
    public string?  DianEstablishmentCode { get; private set; }  // código establecimiento DIAN
    public string?  DianPrefix            { get; private set; }  // prefijo de facturación de esta sucursal

    // Configuración operativa
    public bool     IsMain        { get; private set; }  // sucursal principal
    public bool     IsActive      { get; private set; }
    public int      SortOrder     { get; private set; }

    // TenantId de Finbuckle (BaseEntity)
}
```

**Índices:** `(Code + TenantId) UNIQUE`

**Seed para Tecnoimportaciones:**
```
BOG → "Bogotá"    → IsMain: true
MED → "Medellín"  → IsMain: false
```

---

### 22.2 CostCenter (Centros de costo)

```csharp
// Unidades para imputar ingresos y gastos
// Permite P&L por área, por sucursal, por proyecto o por línea de producto

public class CostCenter : BaseEntity
{
    // Jerarquía (árbol recursivo — igual que Category)
    public Guid?    ParentId      { get; private set; }  // null = centro raíz
    public CostCenter? Parent     { get; private set; }
    public ICollection<CostCenter> Children { get; private set; }

    // Identificación
    public string   Code          { get; private set; }  // "VEN", "VEN-BOG", "OPS", "MKT"
    public string   Name          { get; private set; }  // "Ventas Bogotá", "Marketing"
    public string?  Description   { get; private set; }

    // Clasificación
    public CostCenterType Type    { get; private set; }
    //  Revenue  → genera ingresos (ventas, servicios)
    //  Cost     → genera costos (operaciones, logística)
    //  Profit   → centro de utilidad (combina ingresos y costos)
    //  Support  → área de soporte (admin, RRHH, TI)

    // Vinculación opcional con sucursal
    public Guid?    BranchId      { get; private set; }

    // Control
    public bool     IsActive      { get; private set; }
    public int      SortOrder     { get; private set; }
    // TenantId de Finbuckle (BaseEntity)
}

public enum CostCenterType { Revenue, Cost, Profit, Support }
```

**Índices:** `(Code + TenantId) UNIQUE`, `ParentId`

**Seed para Tecnoimportaciones:**
```
VEN          → "Ventas"           → Type: Revenue
  VEN-BOG    → "Ventas Bogotá"    → Type: Revenue → ParentId: VEN → BranchId: BOG
  VEN-MED    → "Ventas Medellín"  → Type: Revenue → ParentId: VEN → BranchId: MED
  VEN-ONLINE → "Ventas Online"    → Type: Revenue → ParentId: VEN
OPS          → "Operaciones"      → Type: Cost
  OPS-LOG    → "Logística"        → Type: Cost    → ParentId: OPS
  OPS-BOD    → "Bodega"           → Type: Cost    → ParentId: OPS
MKT          → "Marketing"        → Type: Cost
ADM          → "Administración"   → Type: Support
  ADM-FIN    → "Finanzas"         → Type: Support → ParentId: ADM
  ADM-TI     → "Tecnología"       → Type: Support → ParentId: ADM
EST          → "Maka Studios"     → Type: Revenue
```

---

### 22.3 PaymentMethod (Métodos de pago configurables)

```csharp
// Métodos de pago que acepta o usa el tenant
// Se usan en documentos de venta, compra y cartera

public class PaymentMethod : BaseEntity
{
    public string   Code          { get; private set; }  // "CASH", "TRANSFER", "CARD", "NEQUI"
    public string   Name          { get; private set; }  // "Efectivo", "Transferencia", "Nequi"
    public string?  Description   { get; private set; }
    public PaymentMethodType Type { get; private set; }
    public bool     RequiresReference { get; private set; }  // ¿requiere # referencia/transacción?
    public bool     IsForSales    { get; private set; }  // disponible en ventas
    public bool     IsForPurchases{ get; private set; }  // disponible en compras
    public bool     IsActive      { get; private set; }
    public int      SortOrder     { get; private set; }
    // TenantId de Finbuckle (BaseEntity) — null = global disponible para todos
}

public enum PaymentMethodType
{
    Cash,           // Efectivo
    BankTransfer,   // Transferencia bancaria
    Check,          // Cheque
    CreditCard,     // Tarjeta de crédito
    DebitCard,      // Tarjeta débito
    DigitalWallet,  // Nequi, Daviplata, PSE
    PaymentGateway, // Wompi, PayU (integrado)
    Credit,         // Crédito (a plazo)
    Barter,         // Trueque/compensación
    Other
}
```

**Seed global (disponibles para todos los tenants):**
```
CASH      → Efectivo          → Cash          → IsForSales: true, IsForPurchases: true
TRANSFER  → Transferencia     → BankTransfer  → RequiresReference: true
CHECK     → Cheque            → Check         → RequiresReference: true
NEQUI     → Nequi             → DigitalWallet
DAVIPLATA → Daviplata         → DigitalWallet
PSE       → PSE               → DigitalWallet → RequiresReference: true
WOMPI     → Wompi             → PaymentGateway→ RequiresReference: true
PAYU      → PayU              → PaymentGateway→ RequiresReference: true
CREDIT    → Crédito           → Credit        → IsForSales: true
CARD      → Tarjeta           → CreditCard
```

---

### 22.4 BankAccount (Cuentas bancarias del tenant)

```csharp
// Cuentas bancarias propias del tenant para recibir pagos y hacer pagos

public class BankAccount : BaseEntity
{
    public string   BankName      { get; private set; }  // "Bancolombia", "Davivienda", "BBVA"
    public string   AccountNumber { get; private set; }  // número de cuenta
    public BankAccountType AccountType { get; private set; }
    public string   AccountHolder { get; private set; }  // titular (razón social o nombre)
    public string?  AccountHolderDocument { get; private set; }  // NIT o CC del titular
    public string   Currency      { get; private set; }  // "COP", "USD"
    public bool     IsDefault     { get; private set; }  // cuenta principal para recibos
    public bool     IsActive      { get; private set; }

    // Vinculación con sucursal (opcional)
    public Guid?    BranchId      { get; private set; }

    // Para conciliación bancaria futura
    public string?  SwiftCode     { get; private set; }
    public string?  Iban          { get; private set; }

    // TenantId de Finbuckle (BaseEntity)
}

public enum BankAccountType { Checking, Savings }
```

---

### 22.5 Warehouse (Bodegas — vinculadas a la empresa)

```csharp
// Las bodegas son unidades físicas de almacenamiento del tenant
// Se vinculan con la sucursal y con las direcciones del Party
// El stock en el módulo Inventory referencia siempre a un WarehouseId

public class Warehouse : BaseEntity
{
    public string   Code          { get; private set; }  // "BOD-BOG-1", "BOD-MED-1"
    public string   Name          { get; private set; }  // "Bodega Bogotá Principal"
    public string?  Description   { get; private set; }

    // Vinculaciones
    public Guid?    BranchId      { get; private set; }  // sucursal a la que pertenece
    public Guid?    AddressId     { get; private set; }  // dirección física (PartyAddress)

    // Configuración
    public WarehouseType Type     { get; private set; }
    public bool     IsDefault     { get; private set; }  // bodega por defecto para recepciones
    public bool     IsActive      { get; private set; }

    // Control de ubicaciones (para WMS futuro)
    public bool     HasLocations  { get; private set; }  // ¿gestiona ubicaciones internas?

    // TenantId de Finbuckle (BaseEntity)
}

public enum WarehouseType
{
    Main,       // bodega principal
    Transit,    // bodega de tránsito (productos en movimiento)
    Returns,    // bodega de devoluciones
    Damaged,    // bodega de productos dañados/garantías
    Virtual     // bodega virtual (dropshipping, en camino)
}
```

**Índices:** `(Code + TenantId) UNIQUE`

**Seed para Tecnoimportaciones:**
```
BOD-BOG-1 → "Bodega Bogotá"    → Main    → BranchId: BOG → IsDefault: true
BOD-MED-1 → "Bodega Medellín"  → Main    → BranchId: MED
BOD-TRA   → "En Tránsito"      → Transit → IsDefault: false
BOD-DEV   → "Devoluciones"     → Returns
```

---

### 22.6 SalesPerson (Vendedores del tenant)

```csharp
// Vendedores registrados — vinculados a un usuario del sistema
// Un usuario puede ser vendedor y también tener otros roles (admin, etc.)

public class SalesPerson : BaseEntity
{
    public Guid     UserId        { get; private set; }  // FK a Identity.User
    public string   Code          { get; private set; }  // "V001", "V002" — código interno
    public string   Name          { get; private set; }  // nombre visible (puede diferir del User)

    // Vinculación opcional con sucursal
    public Guid?    BranchId      { get; private set; }

    // Metas (para reportes de ventas futuros)
    public decimal? MonthlySalesTarget { get; private set; }

    // Estado
    public bool     IsActive      { get; private set; }
    // TenantId de Finbuckle (BaseEntity)
}
```

**Índices:** `(UserId + TenantId) UNIQUE`, `(Code + TenantId) UNIQUE`

---

## 23. ACTUALIZACIÓN DE MIGRACIONES

Agregar a las migraciones del §13:

```
Migración 3b: AddCompanyMasters (después de AddPartyBase)
  → Branch
  → CostCenter
  → PaymentMethod (seed global incluido)
  → BankAccount
  → Warehouse
  → SalesPerson
```

**Orden final de migraciones:**
```
1. AddGeography
2. AddFiscalCatalogs
3. AddPartyBase
3b. AddCompanyMasters        ← NUEVO
4. AddTenantAccount
5. AddBusinessPartner
6. AddSupplierAgreements
```

---

## 24. FEATURES ADICIONALES — MAESTROS CONTABLES

### Backend

| Feature | Tipo | Notas |
|---|---|---|
| `CreateBranch` | Command | Validar código único por tenant |
| `UpdateBranch` | Command | |
| `GetBranches` | Query | Lista activa del tenant |
| `CreateCostCenter` | Command | Con soporte de árbol (ParentId) |
| `UpdateCostCenter` | Command | Mover de rama si cambia ParentId |
| `GetCostCenterTree` | Query | Árbol completo del tenant |
| `CreateWarehouse` | Command | Validar código único por tenant |
| `UpdateWarehouse` | Command | |
| `GetWarehouses` | Query | Lista activa del tenant |
| `CreateSalesPerson` | Command | Vincular con usuario existente |
| `GetSalesPersons` | Query | Lista activa del tenant |
| `ManagePaymentMethods` | Command | Activar/desactivar métodos de pago |
| `ManageBankAccounts` | Command | CRUD cuentas bancarias |

### Frontend — Settings

| Ruta | Descripción |
|---|---|
| `/settings/branches` | CRUD de sucursales |
| `/settings/warehouses` | CRUD de bodegas |
| `/settings/cost-centers` | Árbol de centros de costo |
| `/settings/payment-methods` | Activar/configurar métodos de pago |
| `/settings/bank-accounts` | CRUD cuentas bancarias |
| `/settings/sales-persons` | Equipo comercial vinculado a usuarios |

---

## 25. CONEXIONES ACTUALIZADAS CON OTROS MÓDULOS

```
INVENTORY:
  Warehouse.Id        → StockEntry.WarehouseId (módulo Inventory)
  Warehouse.Id        → StockMovement.WarehouseId
  Branch.Id           → filtros de reportes de inventario por sucursal

ORDERS (futuro):
  Branch.Id           → Order.BranchId (desde qué sucursal se vende)
  CostCenter.Id       → Order.CostCenterId (imputación del ingreso)
  Warehouse.Id        → Order.WarehouseId (desde dónde se despacha)
  SalesPerson.Id      → Order.SalesPersonId (resuelto por SalesAssignment)
  PaymentMethod.Id    → OrderPayment.MethodId

BILLING (futuro):
  Branch.DianPrefix   → numeración de factura por sucursal
  BankAccount.Id      → Invoice.PaymentAccountId (cuenta para pagar)
  CostCenter.Id       → InvoiceLine.CostCenterId (imputación por línea)

ACCOUNTING (futuro):
  CostCenter.Id       → JournalEntry.CostCenterId
  Branch.Id           → JournalEntry.BranchId
  BankAccount.Id      → BankReconciliation.AccountId
```

---

## 26. PERMISOS ADICIONALES

```
Permissions.Settings.Branches.View
Permissions.Settings.Branches.Manage
Permissions.Settings.Warehouses.View
Permissions.Settings.Warehouses.Manage
Permissions.Settings.CostCenters.View
Permissions.Settings.CostCenters.Manage
Permissions.Settings.PaymentMethods.Manage
Permissions.Settings.BankAccounts.View
Permissions.Settings.BankAccounts.Manage   ← datos sensibles
Permissions.Settings.SalesPersons.Manage
```

---

*Versión: 1.1 | Módulo: Parties | Mayo 2026*
*Actualización: Secciones 22-26 — Datos Maestros Contables*
*(Branch, CostCenter, PaymentMethod, BankAccount, Warehouse, SalesPerson)*
