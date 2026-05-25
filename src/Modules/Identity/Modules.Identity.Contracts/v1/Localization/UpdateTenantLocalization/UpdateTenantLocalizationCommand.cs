using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Localization.UpdateTenantLocalization;

/// <summary>
/// Upserts the localization settings for the caller's current tenant.
/// Requires the Settings.Localization.Update permission.
/// </summary>
public sealed record UpdateTenantLocalizationCommand(
    string Timezone,
    string DateFormat,
    string TimeFormat,
    string Currency,
    string Language,
    string NumberFormat
) : ICommand<TenantLocalizationDto>;
