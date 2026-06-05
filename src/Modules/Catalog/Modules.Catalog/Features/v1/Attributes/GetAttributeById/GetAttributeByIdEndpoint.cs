using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributeById;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.GetAttributeById;

public static class GetAttributeByIdEndpoint
{
    public static RouteHandlerBuilder MapGetAttributeByIdEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                    TypedResults.Ok(await mediator.Send(new GetAttributeByIdQuery(id), cancellationToken)))
            .WithName("GetAttributeById")
            .WithSummary("Get attribute by id")
            .WithDescription("Returns a catalog attribute with its values.")
            .RequirePermission(CatalogPermissions.Attributes.View)
            .Produces<AttributeDetailDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
