using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Contracts.v1.Parties;

// Exposición v2 ADITIVA (PR-D5d). Sub-objeto anidado para que el cutover de Catalog (PR-E) sea un
// swap claro (party.Kind → party.V2.TipoPersona…) y la frontera v1↔v2 quede explícita. Los DTOs v1
// (PartyDto, PartyDetailDto) solo ganan un parámetro V2 nullable al final (default null) → v1 byte-idéntico.

/// <summary>Resumen v2 liviano para el listado (solo campos inline en la fila Party — cero joins).</summary>
public sealed record PartyV2SummaryDto(
    RegimenTributario?  RegimenTributario,
    ResponsabilidadIVA? ResponsabilidadIVA,
    Guid?               ParentPartyId);

/// <summary>Exposición v2 completa para el detalle.</summary>
public sealed record PartyV2DetailDto(
    Guid?                              ParentPartyId,
    PartyV2FiscalDto?                  Fiscal,
    PartyV2LegalRepDto?                LegalRepresentative,
    PartyV2CustomerProfileDto?         Customer,
    PartyV2SupplierProfileDto?         Supplier,
    PartyV2ContactProfileDto?          Contact,
    PartyV2PartnerProfileDto?          Partner,
    PartyV2EmployeeProfileDto?         Employee,
    IReadOnlyList<PartyV2CiiuDto>      CiiuActivities,
    PartyV2CreditDto?                  Credit,
    IReadOnlyList<PartyV2HoldDto>      ActiveHolds);

public sealed record PartyV2FiscalDto(
    RegimenTributario?  RegimenTributario,
    ResponsabilidadIVA? ResponsabilidadIVA,
    bool                GranContribuyente,
    bool                Autorretenedor,
    bool                AgenteRetencionIVA,
    bool                AgenteRetencionICA,
    bool                ObligadoLlevarContabilidad,
    bool                FlagPEP,          // DEPRECADO (PR-2): PEP vive en Party.IsPEP. Ver DEPRECATIONS.md.
    string?             FormaJuridica,
    IReadOnlyList<string> ResponsabilidadesFiscales,
    string?             EmailFacturacion = null);  // PR-2: email DIAN dedicado

public sealed record PartyV2LegalRepDto(
    string             Nombres,
    string             Apellidos,
    TipoIdentificacion TipoIdentificacion,
    string             NumeroIdentificacion,
    string?            Telefono,
    string?            Celular,
    string?            Email,
    bool               EsPEP);

public sealed record PartyV2CustomerProfileDto(
    Guid?    ClassificationId,
    Guid?    PriceListId,
    Guid?    DefaultSalespersonId,
    decimal? MaxDiscountPct,
    bool     AllowDiscount,
    bool     IsActive);

public sealed record PartyV2SupplierProfileDto(
    Guid?  ClassificationId,
    Guid?  DefaultCurrencyId,
    int    DiasCredito,
    bool   IsDropshipping,
    int?   LeadTimeDays,
    bool   IsActive);

/// <summary>Faceta contacto adelgazada (PR-2): solo atributos globales de la persona. Lo por-empresa
/// (cargo/función/principal) vive en <c>PartyRelationship</c>.</summary>
public sealed record PartyV2ContactProfileDto(
    Guid? ResponsibleUserId);

public sealed record PartyV2PartnerProfileDto(
    decimal   SharePercentage,
    DateOnly  StartDate,
    DateOnly? EndDate,
    string?   Status);

public sealed record PartyV2EmployeeProfileDto(
    string? EmployeeCode,
    string? JobTitle,
    bool    IsSalesperson,
    bool    IsCollector,
    bool    IsActive);

public sealed record PartyV2CiiuDto(string CiiuCode, bool IsPrincipal);

/// <summary>
/// Crédito v2. <see cref="SaldoDisponible"/> es PROVISIONAL en M1: hoy == <see cref="CupoAsignado"/>
/// (sin consumos). Su semántica cambia en M2 cuando Contabilidad reste las facturas pendientes; una
/// UI NO debe mostrarlo como disponible definitivo todavía.
/// </summary>
public sealed record PartyV2CreditDto(
    decimal CupoAsignado,
    decimal SaldoDisponible,
    bool    EstaActivo,
    string  MonedaId,
    int     DiasCredito);

public sealed record PartyV2HoldDto(
    HoldType  HoldType,
    string    Motivo,
    DateOnly  FechaInicio,
    DateOnly? FechaLiberacion);
