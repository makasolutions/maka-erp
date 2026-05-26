using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Appearance.UpdateTenantAppearance;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Appearance.UpdateTenantAppearance;

public static class UpdateTenantAppearanceEndpoint
{
    public static RouteHandlerBuilder MapUpdateTenantAppearanceEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/appearance",
            async (UpdateTenantAppearanceCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(command, cancellationToken)))
            .WithName("UpdateTenantAppearance")
            .WithSummary("Update appearance settings for the current tenant")
            .WithDescription("Upserts theme, accent, font, density, and optional custom accent JSON for the caller's tenant. Requires Settings.Appearance.Update permission.")
            .RequirePermission(IdentityPermissions.Appearance.Update)
            .Produces<TenantAppearanceDto>(StatusCodes.Status200OK);
    }
}
