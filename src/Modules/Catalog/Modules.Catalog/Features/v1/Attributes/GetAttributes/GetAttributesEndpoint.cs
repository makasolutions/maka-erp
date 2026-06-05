using FSH.Framework.Shared.Identity.Authorization;
using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributes;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.GetAttributes;

public static class GetAttributesEndpoint
{
    public static RouteHandlerBuilder MapGetAttributesEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/",
                async ([AsParameters] GetAttributesQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(query, cancellationToken)))
            .WithName("GetAttributes")
            .WithSummary("List catalog attributes")
            .WithDescription("Returns a paginated list of catalog attributes for the current tenant.")
            .RequirePermission(CatalogPermissions.Attributes.View)
            .Produces<PagedResponse<AttributeDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
