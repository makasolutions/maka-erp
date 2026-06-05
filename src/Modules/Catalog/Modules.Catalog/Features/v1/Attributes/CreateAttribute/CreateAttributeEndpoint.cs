using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.Attributes.CreateAttribute;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FSH.Modules.Catalog.Features.v1.Attributes.CreateAttribute;

public static class CreateAttributeEndpoint
{
    public static RouteHandlerBuilder MapCreateAttributeEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/",
                async (CreateAttributeCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return TypedResults.Created($"/api/v1/catalog/attributes/{id}", id);
                })
            .WithName("CreateAttribute")
            .WithSummary("Create catalog attribute")
            .WithDescription("Creates a new catalog attribute (e.g. Color, Size) for the current tenant.")
            .RequirePermission(CatalogPermissions.Attributes.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status409Conflict)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
}
