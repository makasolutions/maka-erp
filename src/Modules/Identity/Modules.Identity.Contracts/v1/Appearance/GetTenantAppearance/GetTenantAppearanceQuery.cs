using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Appearance.GetTenantAppearance;

/// <summary>
/// Returns the appearance settings for the caller's current tenant.
/// Any authenticated user can call this — it drives theme, accent, font, and
/// density across the dashboard and is therefore treated as a basic read.
/// </summary>
public sealed record GetTenantAppearanceQuery : IQuery<TenantAppearanceDto>;
