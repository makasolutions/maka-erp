using FSH.Framework.Core.Domain;

namespace FSH.Modules.Identity.Domain;

/// <summary>
/// Localization preferences scoped to a tenant.
/// One row per tenant (UserId = null). A future override per user
/// can be added by setting UserId to a non-null value without any
/// schema or handler changes.
/// </summary>
public class TenantLocalization : IAuditableEntity
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Reserved for a future per-user override.
    /// null = settings apply to the entire tenant.
    /// </summary>
    public Guid? UserId { get; private set; }

    /// <summary>IANA timezone identifier, e.g. "America/Bogota".</summary>
    public string Timezone { get; private set; } = default!;

    /// <summary>Display token: "DD/MM/YYYY" | "MM/DD/YYYY" | "YYYY-MM-DD".</summary>
    public string DateFormat { get; private set; } = default!;

    /// <summary>"12h" or "24h".</summary>
    public string TimeFormat { get; private set; } = default!;

    /// <summary>ISO 4217 code: "COP" | "USD" | "EUR".</summary>
    public string Currency { get; private set; } = default!;

    /// <summary>BCP 47 tag: "es" | "en".</summary>
    public string Language { get; private set; } = default!;

    /// <summary>Number style token: "1.000,00" | "1,000.00".</summary>
    public string NumberFormat { get; private set; } = default!;

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private TenantLocalization() { } // EF Core

    public static TenantLocalization CreateDefault(string? createdBy = null) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = null,
            Timezone = "America/Bogota",
            DateFormat = "DD/MM/YYYY",
            TimeFormat = "12h",
            Currency = "COP",
            Language = "es",
            NumberFormat = "1.000,00",
            CreatedOnUtc = TimeProvider.System.GetUtcNow(),
            CreatedBy = createdBy,
        };

    public void Update(
        string timezone,
        string dateFormat,
        string timeFormat,
        string currency,
        string language,
        string numberFormat,
        string? modifiedBy = null)
    {
        Timezone = timezone;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        Currency = currency;
        Language = language;
        NumberFormat = numberFormat;
        LastModifiedOnUtc = TimeProvider.System.GetUtcNow();
        LastModifiedBy = modifiedBy;
    }
}
