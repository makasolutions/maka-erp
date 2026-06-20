using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Relationships;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Relationships.DeletePartyRelationship;

public sealed class DeletePartyRelationshipCommandValidator : AbstractValidator<DeletePartyRelationshipCommand>
{
    public DeletePartyRelationshipCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeletePartyRelationshipCommandHandler(PartiesDbContext db)
    : ICommandHandler<DeletePartyRelationshipCommand>
{
    public async ValueTask<Unit> Handle(DeletePartyRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var relationship = await db.PartyRelationships
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Vínculo no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        // Baja lógica: no borra la persona ni sus otras relaciones.
        relationship.Deactivate(DateOnly.FromDateTime(DateTime.UtcNow));
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class DeletePartyRelationshipEndpoint
{
    public static RouteHandlerBuilder MapDeletePartyRelationshipEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/relationships/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeletePartyRelationshipCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeletePartyRelationship")
            .WithSummary("Deactivate a person-company relationship (logical)")
            .RequirePermission(PartiesPermissions.Relationships.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
