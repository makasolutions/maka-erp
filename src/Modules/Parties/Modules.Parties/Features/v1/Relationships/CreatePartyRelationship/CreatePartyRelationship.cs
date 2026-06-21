using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.Relationships;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain.CustomFields;
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
        RuleFor(x => x.TargetPartyId).NotEmpty();
        // PR-3: persona discriminada — exactamente una de SourcePartyId | NewPerson.
        RuleFor(x => x)
            .Must(x => (x.SourcePartyId is { } id && id != Guid.Empty) ^ (x.NewPerson is not null))
            .WithMessage("Indique una persona existente O una persona nueva (no ambas, no ninguna).");
        RuleFor(x => x.SourcePartyId).NotEqual(x => x.TargetPartyId)
            .When(x => x.SourcePartyId.HasValue)
            .WithMessage("Una persona no puede vincularse a sí misma.");
        When(x => x.NewPerson is not null, () =>
        {
            RuleFor(x => x.NewPerson!.IdentificationTypeCode).NotEmpty();
            RuleFor(x => x.NewPerson!.IdentificationNumber).NotEmpty();
        });
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

        // Resolver-o-crear la persona (existente reusa; nueva inline se crea en esta misma transacción).
        Guid sourceId = await RelationshipWriteSupport
            .ResolveOrCreatePersonAsync(db, command.SourcePartyId, command.NewPerson, cancellationToken)
            .ConfigureAwait(false);
        if (sourceId == command.TargetPartyId)
            throw new CustomException("Una persona no puede vincularse a sí misma.",
                Enumerable.Empty<string>(), HttpStatusCode.BadRequest);

        // Invariante "un principal activo por empresa": desmarca el anterior antes de insertar el nuevo
        // (en la misma transacción) para no violar ix_partyrel_primary.
        if (command.IsPrimary)
        {
            var current = await db.PartyRelationships
                .Where(r => r.TargetPartyId == command.TargetPartyId && r.IsActive && r.IsPrimary)
                .ToListAsync(cancellationToken).ConfigureAwait(false);
            foreach (var r in current) r.SetPrimary(false);
        }

        var customFields = await RelationshipWriteSupport
            .BuildCustomFieldsAsync(db, command.CustomFields, CustomFieldCompletenessMode.Minimal, cancellationToken)
            .ConfigureAwait(false);

        var relationship = PartyRelationship.Create(
            sourceId, command.TargetPartyId, command.RelationshipTypeCode,
            command.ContactFunctionCode, command.JobTitleCode, command.IsPrimary,
            command.StartDate, command.EndDate);
        relationship.SetCustomFields(customFields);

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
