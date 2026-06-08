using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Contracts.v1.Agreements;

/// <summary>Fila de listado de convenios.</summary>
public sealed record AgreementDto(
    Guid            Id,
    string          Name,
    Guid            SupplierId,
    string?         SupplierName,
    AgreementType   AgreementType,
    AgreementStatus Status,
    Guid?           PriceListId,
    string?         PriceListName,
    DateTime        ValidFrom,
    DateTime?       ValidTo,
    int             RuleCount);

/// <summary>Regla de un convenio (entrada y salida comparten forma).</summary>
public sealed record AgreementRuleInput(
    AgreementRuleType RuleType,
    decimal?          NumericValue,
    bool?             BoolValue,
    string?           TextValue,
    bool              IsMandatory);

public sealed record AgreementRuleDto(
    Guid              Id,
    AgreementRuleType RuleType,
    decimal?          NumericValue,
    bool?             BoolValue,
    string?           TextValue,
    bool              IsMandatory);

/// <summary>Detalle completo de un convenio (incluye reglas).</summary>
public sealed record AgreementDetailDto(
    Guid            Id,
    string          Name,
    Guid            SupplierId,
    string?         SupplierName,
    AgreementType   AgreementType,
    AgreementStatus Status,
    Guid?           PriceListId,
    string?         PriceListName,
    Guid?           SuggestedPriceListId,
    string?         SuggestedPriceListName,
    AgreementResponsible DispatchResponsible,
    AgreementResponsible WaybillResponsible,
    AgreementResponsible SettlementResponsible,
    string?         FailedDeliveryPolicy,
    string?         ReturnsPolicy,
    string?         WarrantyPolicy,
    DateTime        ValidFrom,
    DateTime?       ValidTo,
    string?         Notes,
    bool            IsMutable,
    IReadOnlyList<AgreementRuleDto> Rules);

/// <summary>Resultado de evaluar una regla contra un distribuidor.</summary>
public sealed record RuleEvaluationDto(
    AgreementRuleType    RuleType,
    RuleEvaluationResult Result,
    bool                 IsMandatory,
    string               Detail);

/// <summary>Evaluación completa de un convenio frente a un distribuidor.</summary>
public sealed record AgreementEvaluationDto(
    Guid AgreementId,
    Guid DistributorPartyId,
    string? DistributorName,
    bool Eligible,
    IReadOnlyList<RuleEvaluationDto> Rules);
