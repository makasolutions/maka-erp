using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;

public sealed record UpdatePartyCommand(
    Guid        Id,
    int?        VerificationDigit,
    PartyKind   Kind,
    string      LegalName,
    PartyRole   Roles,
    string?     TradeName,
    string?     Email,
    string?     Website,
    string?     TaxRegimeCode,
    string?     FiscalResponsibilities,
    PartyStatus Status,
    LifecycleStage Stage,
    int         LeadScore,
    string?     SourceCode,
    Guid?       AssignedUserId,
    string?     MarketingType,
    DateOnly?   BirthDate,
    string?     GenderCode,
    string?     MaritalStatusCode,
    decimal?    CreditLimit,
    string?     CreditCurrency,
    string?     Notes,
    Guid?       BranchId,
    string?     FirstName = null,
    string?     LastName = null,
    string?     ActividadEconomicaCiiuCode = null,
    bool        HasCredit = false,
    string?     CreditDaysCode = null,
    bool        CreditBlocked = false,
    IReadOnlyList<PartyAddressInput>?    Addresses = null,
    IReadOnlyList<PartyContactInput>?    Contacts = null,
    IReadOnlyList<PartyChannelInput>?    Channels = null,
    IReadOnlyList<PartyTeamMemberInput>? Team = null,
    // Ejes fiscales v2 autoritativos (alternativa al TaxRegimeCode legacy). Default null = legacy.
    PartyFiscalAxesInput? FiscalAxes = null) : ICommand<Guid>;
