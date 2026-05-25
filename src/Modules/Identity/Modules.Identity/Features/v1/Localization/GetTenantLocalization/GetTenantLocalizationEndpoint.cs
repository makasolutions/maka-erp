using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Localization.GetTenantLocalization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Localization.GetTenantLocalization;

public static class GetTenantLocalizationEndpoint
{
    public static RouteHandlerBuilder MapGetTenantLocalizationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/localization",
            async (IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(new GetTenantLocalizationQuery(), cancellationToken)))
            .WithName("GetTenantLocalization")
            .WithSummary("Get localization settings for the current tenant")
            .WithDescription("Returns date, time, currency, language, and number format preferences for the caller's tenant. Seeds defaults (America/Bogota, COP, es) on first call.")
            .RequirePermission(IdentityPermissions.Localization.View)
            .Produces<TenantLocalizationDto>(StatusCodes.Status200OK);
    }
}
