using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.AddAttributeValue;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.AddAttributeValue;

public static class AddAttributeValueEndpoint
{
    public static RouteHandlerBuilder MapAddAttributeValueEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (Guid attributeId, AddAttributeValueCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    var cmd = command with { AttributeId = attributeId };
                    Guid id = await mediator.Send(cmd, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/attributes/{attributeId}/values/{id}", id);
                })
            .WithName("AddAttributeValue")
            .WithSummary("Add value to attribute")
            .WithDescription("Adds a possible value (e.g. Rojo, XL) to a catalog attribute.")
            .RequirePermission(CatalogPermissions.Attributes.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
