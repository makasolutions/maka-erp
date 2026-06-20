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

namespace FSH.Modules.Parties.Features.v1.Relationships.SetPrimaryPartyRelationship;

public sealed class SetPrimaryPartyRelationshipCommandValidator : AbstractValidator<SetPrimaryPartyRelationshipCommand>
{
    public SetPrimaryPartyRelationshipCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class SetPrimaryPartyRelationshipCommandHandler(PartiesDbContext db)
    : ICommandHandler<SetPrimaryPartyRelationshipCommand>
{
    public async ValueTask<Unit> Handle(SetPrimaryPartyRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var relationship = await db.PartyRelationships
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Vínculo no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        if (command.IsPrimary)
        {
            // Desmarca el principal anterior de la misma empresa antes de marcar este (invariante).
            var others = await db.PartyRelationships
                .Where(r => r.TargetPartyId == relationship.TargetPartyId && r.Id != relationship.Id && r.IsActive && r.IsPrimary)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var r in others) r.SetPrimary(false);
        }

        relationship.SetPrimary(command.IsPrimary);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class SetPrimaryPartyRelationshipEndpoint
{
    public static RouteHandlerBuilder MapSetPrimaryPartyRelationshipEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/relationships/{id:guid}/primary",
                async (Guid id, SetPrimaryPartyRelationshipCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("SetPrimaryPartyRelationship")
            .WithSummary("Mark a relationship as the company's primary contact")
            .RequirePermission(PartiesPermissions.Relationships.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
