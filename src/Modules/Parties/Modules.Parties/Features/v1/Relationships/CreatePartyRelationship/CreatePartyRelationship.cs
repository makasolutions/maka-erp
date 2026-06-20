using FluentValidation;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Relationships;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain.Relationships;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Relationships.CreatePartyRelationship;

public sealed class CreatePartyRelationshipCommandValidator : AbstractValidator<CreatePartyRelationshipCommand>
{
    public CreatePartyRelationshipCommandValidator()
    {
        RuleFor(x => x.SourcePartyId).NotEmpty();
        RuleFor(x => x.TargetPartyId).NotEmpty();
        RuleFor(x => x.TargetPartyId).NotEqual(x => x.SourcePartyId)
            .WithMessage("Una persona no puede vincularse a sí misma.");
        RuleFor(x => x.RelationshipTypeCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate!.Value)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("La fecha de fin no puede ser anterior al inicio.");
    }
}

public sealed class CreatePartyRelationshipCommandHandler(PartiesDbContext db)
    : ICommandHandler<CreatePartyRelationshipCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreatePartyRelationshipCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Invariante "un principal activo por empresa": desmarca el anterior antes de insertar el nuevo
        // (en la misma transacción) para no violar ix_partyrel_primary.
        if (command.IsPrimary)
        {
            var current = await db.PartyRelationships
                .Where(r => r.TargetPartyId == command.TargetPartyId && r.IsActive && r.IsPrimary)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var r in current) r.SetPrimary(false);
        }

        var relationship = PartyRelationship.Create(
            command.SourcePartyId, command.TargetPartyId, command.RelationshipTypeCode,
            command.ContactFunctionCode, command.JobTitleCode, command.IsPrimary,
            command.StartDate, command.EndDate);

        db.PartyRelationships.Add(relationship);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return relationship.Id;
    }
}

public static class CreatePartyRelationshipEndpoint
{
    public static RouteHandlerBuilder MapCreatePartyRelationshipEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/relationships",
                async (CreatePartyRelationshipCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return Results.Created($"/api/v1/parties/relationships/{id}", id);
                })
            .WithName("CreatePartyRelationship")
            .WithSummary("Link a person to a company (M2M relationship)")
            .RequirePermission(PartiesPermissions.Relationships.Manage)
            .Produces<Guid>(StatusCodes.Status201Created);
}
