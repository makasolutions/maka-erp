using FSH.Modules.Identity.Contracts.DTOs;
using Mediator;

namespace FSH.Modules.Identity.Contracts.v1.Localization.GetTenantLocalization;

/// <summary>
/// Returns the localization settings for the caller's current tenant.
/// Any authenticated user can call this — it drives date/number formatting
/// across the dashboard and is therefore treated as a basic read permission.
/// </summary>
public sealed record GetTenantLocalizationQuery : IQuery<TenantLocalizationDto>;
