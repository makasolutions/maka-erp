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

namespace FSH.Modules.Parties.Features.v1.Relationships.UpdatePartyRelationship;

public sealed class UpdatePartyRelationshipCommandValidator : AbstractValidator<UpdatePartyRelationshipCommand>
{
    public UpdatePartyRelationshipCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.RelationshipTypeCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .When(x => x.EndDate.HasValue)
            .WithMessage("La fecha de fin no puede ser anterior al inicio.");
    }
}

public sealed class UpdatePartyRelationshipCommandHandler(PartiesDbContext db)
    : ICommandHandler<UpdatePartyRelationshipCommand>
{
    public async ValueTask<Unit> Handle(UpdatePartyRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var relationship = await db.PartyRelationships
            .FirstOrDefaultAsync(r => r.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Vínculo no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        relationship.Update(command.RelationshipTypeCode, command.ContactFunctionCode, command.JobTitleCode,
            command.StartDate, command.EndDate);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class UpdatePartyRelationshipEndpoint
{
    public static RouteHandlerBuilder MapUpdatePartyRelationshipEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/relationships/{id:guid}",
                async (Guid id, UpdatePartyRelationshipCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdatePartyRelationship")
            .WithSummary("Edit a person-company relationship")
            .RequirePermission(PartiesPermissions.Relationships.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
