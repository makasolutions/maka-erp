using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Contracts.v1.Relationships;
using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;

public sealed record CreatePartyCommand(
    string      IdentificationTypeCode,
    string      IdentificationNumber,
    int?        VerificationDigit,
    PartyKind   Kind,
    string      LegalName,
    PartyRole   Roles,
    string?     TradeName = null,
    string?     Email = null,
    string?     Website = null,
    string?     TaxRegimeCode = null,
    string?     FiscalResponsibilities = null,
    PartyStatus Status = PartyStatus.Active,
    LifecycleStage Stage = LifecycleStage.Lead,
    int         LeadScore = 0,
    string?     SourceCode = null,
    Guid?       AssignedUserId = null,
    string?     MarketingType = null,
    DateOnly?   BirthDate = null,
    string?     GenderCode = null,
    string?     MaritalStatusCode = null,
    decimal?    CreditLimit = null,
    string?     CreditCurrency = null,
    string?     Notes = null,
    Guid?       BranchId = null,
    string?     FirstName = null,
    string?     LastName = null,
    string?     ActividadEconomicaCiiuCode = null,
    bool        HasCredit = false,
    string?     CreditDaysCode = null,
    bool        CreditBlocked = false,
    IReadOnlyList<PartyAddressInput>?    Addresses = null,
    IReadOnlyList<PartyChannelInput>?    Channels = null,
    IReadOnlyList<PartyTeamMemberInput>? Team = null,
    // Ejes fiscales v2 autoritativos (alternativa al TaxRegimeCode legacy). Default null = legacy.
    PartyFiscalAxesInput? FiscalAxes = null,
    // PR-3 (Opción B): contactos (vínculos M2M) acumulados en el wizard y persistidos en la MISMA
    // transacción que la empresa. Cada línea referencia persona existente o nueva inline.
    IReadOnlyList<PartyRelationshipLineInput>? Relationships = null) : ICommand<Guid>;
