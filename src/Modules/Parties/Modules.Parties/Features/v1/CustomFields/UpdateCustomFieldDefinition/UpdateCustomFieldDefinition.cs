using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.CustomFields;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain.CustomFields;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.CustomFields.UpdateCustomFieldDefinition;

public sealed class UpdateCustomFieldDefinitionCommandValidator : AbstractValidator<UpdateCustomFieldDefinitionCommand>
{
    public UpdateCustomFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.DefaultValue).MaximumLength(1024);
    }
}

public sealed class UpdateCustomFieldDefinitionCommandHandler(PartiesDbContext db)
    : ICommandHandler<UpdateCustomFieldDefinitionCommand>
{
    public async ValueTask<Unit> Handle(UpdateCustomFieldDefinitionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var definition = await db.CustomFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Definición de campo no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        // EntityType / ApiSlug / FieldType son inmutables: Update no los toca (los referencian los
        // valores ya guardados). Select/MultiSelect siguen exigiendo opciones (lo valida la entidad).
        definition.Update(
            command.Title, command.Description, command.IsRequired, command.IsUnique,
            command.IsDefaultValueEnabled, command.DefaultValue, command.IsMultiselect,
            command.Options.ToDomain());

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class UpdateCustomFieldDefinitionEndpoint
{
    public static RouteHandlerBuilder MapUpdateCustomFieldDefinitionEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPut(
                "/custom-fields/{id:guid}",
                async (Guid id, UpdateCustomFieldDefinitionCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    if (id != command.Id) return Results.BadRequest("Route id mismatch.");
                    await mediator.Send(command, cancellationToken);
                    return Results.NoContent();
                })
            .WithName("UpdateCustomFieldDefinition")
            .WithSummary("Edit a custom field definition (admin)")
            .RequirePermission(PartiesPermissions.CustomFields.Define)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
