namespace FSH.Modules.Identity.Contracts.DTOs;

/// <summary>
/// Localization preferences for the current tenant.
/// Controls how dates (DateFormat), times (TimeFormat), numbers (NumberFormat),
/// and currency (Currency) are displayed across the dashboard.
/// Timezone is an IANA identifier. Language is a BCP 47 tag.
/// </summary>
public sealed record TenantLocalizationDto(
    string Timezone,
    string DateFormat,
    string TimeFormat,
    string Currency,
    string Language,
    string NumberFormat
);
