using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.Profiles;

/// <summary>
/// Faceta cliente de un tercero (SPEC §6.1). Solo configuración de la faceta — el crédito vive en
/// <c>CreditAccount</c> (§9) y la clasificación es FK a catálogo. NO duplica términos de pago/crédito.
///
/// PR-A: entidad independiente con <c>PartyId</c> Guid (sin nav en Party). Activación idempotente.
/// </summary>
public sealed class CustomerProfile : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public Guid? ClassificationId { get; private set; }
    public Guid? PriceListId { get; private set; }
    public Guid? DefaultSalespersonId { get; private set; }
    public Guid? DefaultBranchId { get; private set; }
    public Guid? WithholdingRuleId { get; private set; }
    public decimal? MaxDiscountPct { get; private set; }
    public bool AllowDiscount { get; private set; }
    public Guid? MarketingSourceId { get; private set; }
    public DateTimeOffset ActivatedOn { get; private set; }
    public bool IsActive { get; private set; }

    private CustomerProfile() { }

    public static CustomerProfile Create(
        Guid partyId,
        Guid? classificationId = null,
        Guid? priceListId = null,
        Guid? defaultSalespersonId = null,
        Guid? defaultBranchId = null,
        Guid? withholdingRuleId = null,
        decimal? maxDiscountPct = null,
        bool allowDiscount = false,
        Guid? marketingSourceId = null,
        DateTimeOffset? now = null)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        return new CustomerProfile
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            ClassificationId = classificationId,
            PriceListId = priceListId,
            DefaultSalespersonId = defaultSalespersonId,
            DefaultBranchId = defaultBranchId,
            WithholdingRuleId = withholdingRuleId,
            MaxDiscountPct = maxDiscountPct,
            AllowDiscount = allowDiscount,
            MarketingSourceId = marketingSourceId,
            ActivatedOn = now ?? DateTimeOffset.UtcNow,
            IsActive = true,
        };
    }

    /// <summary>Activa la faceta. Idempotente: si ya está activa, no cambia el estado ni la fecha.</summary>
    public void Activate(DateTimeOffset? now = null)
    {
        if (IsActive) return;
        IsActive = true;
        ActivatedOn = now ?? DateTimeOffset.UtcNow;
    }

    /// <summary>Desactiva la faceta (soft). Idempotente. Preserva el historial.</summary>
    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
    }
}
