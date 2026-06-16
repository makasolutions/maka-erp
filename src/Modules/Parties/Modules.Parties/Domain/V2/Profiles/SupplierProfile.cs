using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.V2.Profiles;

/// <summary>Términos de pago de un proveedor (owned VO de <see cref="SupplierProfile"/>, SPEC §6.2).</summary>
public sealed record PaymentTerms
{
    public int DiasCredito { get; init; }
    public Guid? FormaPagoId { get; init; }

    public static PaymentTerms None { get; } = new();
}

/// <summary>
/// Faceta proveedor de un tercero (SPEC §6.2). PR-A: entidad independiente con <c>PartyId</c> Guid.
/// Activación idempotente.
/// </summary>
public sealed class SupplierProfile : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public Guid? ClassificationId { get; private set; }
    /// <summary>
    /// Moneda por defecto del proveedor. Nullable porque v1 no tiene un catálogo de monedas con
    /// identificadores Guid; el backfill de PR-C la deja sin asignar y se poblará cuando exista
    /// dicho catálogo. Ver SPEC sección 6.2.
    /// </summary>
    public Guid? DefaultCurrencyId { get; private set; }
    public PaymentTerms PaymentTerms { get; private set; } = PaymentTerms.None;
    public Guid? WithholdingRuleId { get; private set; }
    public Guid? DefaultPriceListId { get; private set; }
    public bool IsDropshipping { get; private set; }
    public int? LeadTimeDays { get; private set; }
    public Guid? DefaultBankAccountId { get; private set; }
    public DateTimeOffset ActivatedOn { get; private set; }
    public bool IsActive { get; private set; }

    private SupplierProfile() { }

    public static SupplierProfile Create(
        Guid partyId,
        Guid? defaultCurrencyId = null,
        Guid? classificationId = null,
        PaymentTerms? paymentTerms = null,
        Guid? withholdingRuleId = null,
        Guid? defaultPriceListId = null,
        bool isDropshipping = false,
        int? leadTimeDays = null,
        Guid? defaultBankAccountId = null,
        DateTimeOffset? now = null)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        return new SupplierProfile
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            // Guid.Empty se normaliza a null (sin catálogo de monedas aún — SPEC §6.2).
            DefaultCurrencyId = defaultCurrencyId == Guid.Empty ? null : defaultCurrencyId,
            ClassificationId = classificationId,
            PaymentTerms = paymentTerms ?? PaymentTerms.None,
            WithholdingRuleId = withholdingRuleId,
            DefaultPriceListId = defaultPriceListId,
            IsDropshipping = isDropshipping,
            LeadTimeDays = leadTimeDays,
            DefaultBankAccountId = defaultBankAccountId,
            ActivatedOn = now ?? DateTimeOffset.UtcNow,
            IsActive = true,
        };
    }

    public void Activate(DateTimeOffset? now = null)
    {
        if (IsActive) return;
        IsActive = true;
        ActivatedOn = now ?? DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
    }
}
