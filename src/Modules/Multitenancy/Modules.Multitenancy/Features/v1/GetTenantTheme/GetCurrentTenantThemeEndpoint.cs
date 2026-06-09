using FSH.Modules.Multitenancy.Contracts.Dtos;
using FSH.Modules.Multitenancy.Contracts.v1.GetTenantTheme;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Multitenancy.Features.v1.GetTenantTheme;

/// <summary>
/// Self-service variant of GetTenantTheme: any authenticated user can read the
/// branding (palette + brand assets + typography) of their OWN current tenant so
/// the dashboard can apply the operator-configured branding on sign-in. No operator
/// permission required — the multitenant context already scopes it to the caller's tenant.
/// </summary>
public static class GetCurrentTenantThemeEndpoint
{
    public static RouteHandlerBuilder Map(IEndpointRouteBuilder endpoints)
    {
        return endpoints.MapGet("/theme/current", async (IMediator mediator, CancellationToken cancellationToken) =>
                TypedResults.Ok(await mediator.Send(new GetTenantThemeQuery(), cancellationToken)))
            .WithName("GetCurrentTenantTheme")
            .WithSummary("Get the current tenant's branding (self-service)")
            .WithDescription("Returns the current tenant's theme/branding so its apps can apply it on sign-in. Any authenticated tenant user may read their own tenant branding.")
            .RequireAuthorization()
            .Produces<TenantThemeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }
}
