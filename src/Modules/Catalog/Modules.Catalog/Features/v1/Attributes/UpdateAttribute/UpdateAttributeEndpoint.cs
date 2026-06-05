using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttribute;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttribute;

public static class UpdateAttributeEndpoint
{
    public static RouteHandlerBuilder MapUpdateAttributeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{id:guid}",
                async (Guid id, UpdateAttributeCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { Id = id };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdateAttribute")
            .WithSummary("Update catalog attribute")
            .WithDescription("Updates a catalog attribute's metadata.")
            .RequirePermission(CatalogPermissions.Attributes.Manage)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
