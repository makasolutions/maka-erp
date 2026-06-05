using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.RemoveAttributeValue;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.RemoveAttributeValue;

public static class RemoveAttributeValueEndpoint
{
    public static RouteHandlerBuilder MapRemoveAttributeValueEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/{valueId:guid}",
                async (Guid attributeId, Guid valueId, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    _ = await mediator.Send(new RemoveAttributeValueCommand(attributeId, valueId), cancellationToken);
                    return TypedResults.NoContent();
                })
            .WithName("RemoveAttributeValue")
            .WithSummary("Remove attribute value")
            .WithDescription("Removes a value from a catalog attribute. Fails if the value is selected on any product.")
            .RequirePermission(CatalogPermissions.Attributes.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
