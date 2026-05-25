using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Contracts.DTOs;
using FSH.Modules.Identity.Contracts.v1.Localization.UpdateTenantLocalization;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Identity.Features.v1.Localization.UpdateTenantLocalization;

public static class UpdateTenantLocalizationEndpoint
{
    public static RouteHandlerBuilder MapUpdateTenantLocalizationEndpoint(this IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapPut("/localization",
            async (UpdateTenantLocalizationCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(command, cancellationToken)))
            .WithName("UpdateTenantLocalization")
            .WithSummary("Update localization settings for the current tenant")
            .WithDescription("Upserts timezone, date format, time format, currency, language, and number format for the caller's tenant. Requires Settings.Localization.Update permission.")
            .RequirePermission(IdentityPermissions.Localization.Update)
            .Produces<TenantLocalizationDto>(StatusCodes.Status200OK);
    }
}
