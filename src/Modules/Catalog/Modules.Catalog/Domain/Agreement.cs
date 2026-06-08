using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Convenio comercial con un proveedor (Fase E). Fija la tarifa acordada (FK a
/// <see cref="PriceList"/>, de donde sale el costo del distribuidor en Fase G), los
/// responsables operativos, las políticas y las reglas configurables que un distribuidor
/// debe cumplir. Tenant-aislado. Inmutable cuando <see cref="AgreementStatus.Terminado"/>.
/// </summary>
public sealed class Agreement : BaseEntity<Guid>
{
    public string          Name                  { get; private set; } = default!;
    public Guid            SupplierId            { get; private set; }
    public AgreementType   AgreementType         { get; private set; }
    public AgreementStatus Status                { get; private set; }
    public Guid?           PriceListId           { get; private set; }   // tarifa acordada
    public Guid?           SuggestedPriceListId  { get; private set; }   // PV sugerido

    public AgreementResponsible DispatchResponsible    { get; private set; }
    public AgreementResponsible WaybillResponsible     { get; private set; }
    public AgreementResponsible SettlementResponsible  { get; private set; }

    public string? FailedDeliveryPolicy { get; private set; }
    public string? ReturnsPolicy        { get; private set; }
    public string? WarrantyPolicy       { get; private set; }

    public DateTime  ValidFrom    { get; private set; }
    public DateTime? ValidTo      { get; private set; }
    public string?   Notes        { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private readonly List<AgreementRule> _rules = [];
    public IReadOnlyCollection<AgreementRule> Rules => _rules.AsReadOnly();

    private Agreement() { }

    public static Agreement Create(
        string name,
        Guid supplierId,
        AgreementType agreementType,
        Guid? priceListId,
        Guid? suggestedPriceListId,
        AgreementResponsible dispatchResponsible,
        AgreementResponsible waybillResponsible,
        AgreementResponsible settlementResponsible,
        string? failedDeliveryPolicy,
        string? returnsPolicy,
        string? warrantyPolicy,
        DateTime validFrom,
        DateTime? validTo,
        string? notes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (supplierId == Guid.Empty) throw new ArgumentException("SupplierId requerido.", nameof(supplierId));

        return new Agreement
        {
            Id                    = Guid.CreateVersion7(),
            Name                  = name.Trim(),
            SupplierId            = supplierId,
            AgreementType         = agreementType,
            Status                = AgreementStatus.Borrador,
            PriceListId           = priceListId,
            SuggestedPriceListId  = suggestedPriceListId,
            DispatchResponsible   = dispatchResponsible,
            WaybillResponsible    = waybillResponsible,
            SettlementResponsible = settlementResponsible,
            FailedDeliveryPolicy  = failedDeliveryPolicy?.Trim(),
            ReturnsPolicy         = returnsPolicy?.Trim(),
            WarrantyPolicy        = warrantyPolicy?.Trim(),
            ValidFrom             = validFrom,
            ValidTo               = validTo,
            Notes                 = notes?.Trim(),
            CreatedAtUtc          = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        AgreementType agreementType,
        Guid? priceListId,
        Guid? suggestedPriceListId,
        AgreementResponsible dispatchResponsible,
        AgreementResponsible waybillResponsible,
        AgreementResponsible settlementResponsible,
        string? failedDeliveryPolicy,
        string? returnsPolicy,
        string? warrantyPolicy,
        DateTime validFrom,
        DateTime? validTo,
        string? notes)
    {
        EnsureMutable();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name                  = name.Trim();
        AgreementType         = agreementType;
        PriceListId           = priceListId;
        SuggestedPriceListId  = suggestedPriceListId;
        DispatchResponsible   = dispatchResponsible;
        WaybillResponsible    = waybillResponsible;
        SettlementResponsible = settlementResponsible;
        FailedDeliveryPolicy  = failedDeliveryPolicy?.Trim();
        ReturnsPolicy         = returnsPolicy?.Trim();
        WarrantyPolicy        = warrantyPolicy?.Trim();
        ValidFrom             = validFrom;
        ValidTo               = validTo;
        Notes                 = notes?.Trim();
        UpdatedAtUtc          = DateTime.UtcNow;
    }

    public void ReplaceRules(IEnumerable<AgreementRule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);
        EnsureMutable();
        _rules.Clear();
        foreach (var r in rules) _rules.Add(r);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void ChangeStatus(AgreementStatus status)
    {
        if (Status == AgreementStatus.Terminado)
            throw new InvalidOperationException("Un convenio terminado es inmutable.");
        Status       = status;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>True cuando el convenio aún puede editarse/eliminarse.</summary>
    public bool IsMutable => Status != AgreementStatus.Terminado;

    private void EnsureMutable()
    {
        if (Status == AgreementStatus.Terminado)
            throw new InvalidOperationException("Un convenio terminado es inmutable.");
    }
}

/// <summary>Regla configurable de un convenio (criterio que el distribuidor debe cumplir).</summary>
public sealed class AgreementRule : BaseEntity<Guid>
{
    public Guid              AgreementId  { get; private set; }
    public AgreementRuleType RuleType     { get; private set; }
    public decimal?          NumericValue { get; private set; }
    public bool?             BoolValue    { get; private set; }
    public string?           TextValue    { get; private set; }
    public bool              IsMandatory  { get; private set; }

    private AgreementRule() { }

    public static AgreementRule Create(
        Guid agreementId,
        AgreementRuleType ruleType,
        decimal? numericValue,
        bool? boolValue,
        string? textValue,
        bool isMandatory)
        => new()
        {
            Id           = Guid.CreateVersion7(),
            AgreementId  = agreementId,
            RuleType     = ruleType,
            NumericValue = numericValue,
            BoolValue    = boolValue,
            TextValue    = textValue?.Trim(),
            IsMandatory  = isMandatory,
        };
}
