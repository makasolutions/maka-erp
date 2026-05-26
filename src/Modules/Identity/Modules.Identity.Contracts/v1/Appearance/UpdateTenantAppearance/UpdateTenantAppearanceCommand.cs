using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Appearance.UpdateTenantAppearance;

/// <summary>
/// Upserts the appearance settings for the caller's current tenant.
/// </summary>
public sealed record UpdateTenantAppearanceCommand(
    string Theme,
    string Accent,
    string Font,
    string Density,
    string? CustomAccentJson
) : ICommand<TenantAppearanceDto>;
