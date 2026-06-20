using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;

public sealed record PartyDetailDto(
    Guid        Id,
    string      IdentificationTypeCode,
    string      IdentificationNumber,
    int?        VerificationDigit,
    PartyKind   Kind,
    string      LegalName,
    string?     FirstName,
    string?     LastName,
    string?     TradeName,
    string?     Email,
    string?     Website,
    string?     TaxRegimeCode,
    string?     FiscalResponsibilities,
    string?     ActividadEconomicaCiiuCode,
    PartyRole   Roles,
    PartyStatus Status,
    LifecycleStage Stage,
    int         LeadScore,
    string?     SourceCode,
    Guid?       AssignedUserId,
    string?     MarketingType,
    DateOnly?   BirthDate,
    string?     GenderCode,
    string?     MaritalStatusCode,
    bool        HasCredit,
    decimal?    CreditLimit,
    string?     CreditDaysCode,
    bool        CreditBlocked,
    string?     CreditCurrency,
    string?     Notes,
    Guid?       BranchId,
    bool        IsGlobalSupplier,
    DateTime    CreatedAtUtc,
    IReadOnlyList<PartyAddressDto>    Addresses,
    IReadOnlyList<PartyChannelDto>    Channels,
    IReadOnlyList<PartyTeamMemberDto> Team,
    // PR-D5d: exposición v2 aditiva (default null → v1 byte-idéntico). Cutover Catalog = PR-E.
    PartyV2DetailDto? V2 = null);

public sealed record PartyAddressDto(Guid Id, string Country, string? Department, string? City, string? Line,
    string? Barrio, string? Reference, decimal? Latitude, decimal? Longitude, bool IsPrimary, string? LabelCode,
    string? DepartmentCode = null, string? MunicipalityCode = null, string? NormalizedLine = null);

// PR-2: PartyContactDto ELIMINADO (PartyContact migró a PartyRelationship).

public sealed record PartyChannelDto(Guid Id, string ChannelTypeCode, string Value, string? Reference, bool IsPrimary);

public sealed record PartyTeamMemberDto(Guid Id, Guid UserId, PartyTeamRole Role);
