namespace FSH.Modules.Identity.Contracts.DTOs;

/// <summary>
/// Appearance settings for the current tenant.
/// Controls theme mode, accent colour, font family, layout density, and an
/// optional custom accent JSON blob for the React dashboard colour picker.
/// </summary>
public sealed record TenantAppearanceDto(
    string Theme,
    string Accent,
    string Font,
    string Density,
    string? CustomAccentJson
);
