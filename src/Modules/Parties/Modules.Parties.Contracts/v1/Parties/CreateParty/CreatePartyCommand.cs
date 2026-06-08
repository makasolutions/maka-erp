using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
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
    IReadOnlyList<PartyContactInput>?    Contacts = null,
    IReadOnlyList<PartyChannelInput>?    Channels = null,
    IReadOnlyList<PartyTeamMemberInput>? Team = null) : ICommand<Guid>;
