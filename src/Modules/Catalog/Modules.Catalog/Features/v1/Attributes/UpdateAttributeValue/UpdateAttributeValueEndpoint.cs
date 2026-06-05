using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttributeValue;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttributeValue;

public static class UpdateAttributeValueEndpoint
{
    public static RouteHandlerBuilder MapUpdateAttributeValueEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/{valueId:guid}",
                async (Guid attributeId, Guid valueId, UpdateAttributeValueCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { AttributeId = attributeId, ValueId = valueId };
                    Guid result = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Ok(result);
                })
            .WithName("UpdateAttributeValue")
            .WithSummary("Update attribute value")
            .WithDescription("Updates a possible value of a catalog attribute.")
            .RequirePermission(CatalogPermissions.Attributes.Manage)
            .Produces<Guid>()
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
