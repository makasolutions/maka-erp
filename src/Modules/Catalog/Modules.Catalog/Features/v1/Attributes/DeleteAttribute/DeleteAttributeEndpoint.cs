using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.DeleteAttribute;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.DeleteAttribute;

public static class DeleteAttributeEndpoint
{
    public static RouteHandlerBuilder MapDeleteAttributeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    _ = await mediator.Send(new DeleteAttributeCommand(id), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("DeleteAttribute")
            .WithSummary("Delete catalog attribute")
            .WithDescription("Permanently deletes an attribute and its values. Fails if the attribute is assigned to any product.")
            .RequirePermission(CatalogPermissions.Attributes.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
