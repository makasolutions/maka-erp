using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Agreements;

public sealed record CreateAgreementCommand(
    string          Name,
    Guid            SupplierId,
    AgreementType   AgreementType,
    Guid?           PriceListId,
    Guid?           SuggestedPriceListId,
    AgreementResponsible DispatchResponsible,
    AgreementResponsible WaybillResponsible,
    AgreementResponsible SettlementResponsible,
    string?         FailedDeliveryPolicy,
    string?         ReturnsPolicy,
    string?         WarrantyPolicy,
    DateTime        ValidFrom,
    DateTime?       ValidTo,
    string?         Notes,
    IReadOnlyList<AgreementRuleInput>? Rules) : ICommand<Guid>;

public sealed record UpdateAgreementCommand(
    Guid            Id,
    string          Name,
    AgreementType   AgreementType,
    Guid?           PriceListId,
    Guid?           SuggestedPriceListId,
    AgreementResponsible DispatchResponsible,
    AgreementResponsible WaybillResponsible,
    AgreementResponsible SettlementResponsible,
    string?         FailedDeliveryPolicy,
    string?         ReturnsPolicy,
    string?         WarrantyPolicy,
    DateTime        ValidFrom,
    DateTime?       ValidTo,
    string?         Notes,
    IReadOnlyList<AgreementRuleInput>? Rules) : ICommand<Guid>;

public sealed record ChangeAgreementStatusCommand(Guid Id, AgreementStatus Status) : ICommand;

public sealed record DeleteAgreementCommand(Guid Id) : ICommand;
