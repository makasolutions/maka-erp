using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Contracts.v1.Parties;

/// <summary>Fila de listado de terceros.</summary>
public sealed record PartyDto(
    Guid        Id,
    string      IdentificationTypeCode,
    string      IdentificationNumber,
    int?        VerificationDigit,
    PartyKind   Kind,
    string      LegalName,
    string?     TradeName,
    PartyRole   Roles,
    PartyStatus Status,
    LifecycleStage Stage,
    string?     Email,
    string?     City,
    Guid?       AssignedUserId,
    DateTime    CreatedAtUtc,
    // PR-D5d: resumen v2 aditivo (default null → v1 byte-idéntico). Cutover Catalog = PR-E.
    PartyV2SummaryDto? V2 = null);

/// <summary>Versión liviana para el PartyPicker (Orders/Cotizaciones/Billing).</summary>
public sealed record PartyBriefDto(
    Guid    Id,
    string  LegalName,
    string  IdentificationTypeCode,
    string  IdentificationNumber,
    PartyRole Roles);
