using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Appearance.GetTenantAppearance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Appearance.GetTenantAppearance;

public static class GetTenantAppearanceEndpoint
{
    public static RouteHandlerBuilder MapGetTenantAppearanceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/appearance",
            async (IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(new GetTenantAppearanceQuery(), cancellationToken)))
            .WithName("GetTenantAppearance")
            .WithSummary("Get appearance settings for the current tenant")
            .WithDescription("Returns theme, accent, font, density, and custom accent settings for the caller's tenant. Seeds defaults (system/rose/inter/default) on first call.")
            .RequirePermission(IdentityPermissions.Appearance.View)
            .Produces<TenantAppearanceDto>(StatusCodes.Status200OK);
    }
}
