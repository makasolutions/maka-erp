using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.CustomFields;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.CustomFields.DeactivateCustomFieldDefinition;

public sealed class DeactivateCustomFieldDefinitionCommandValidator : AbstractValidator<DeactivateCustomFieldDefinitionCommand>
{
    public DeactivateCustomFieldDefinitionCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class DeactivateCustomFieldDefinitionCommandHandler(PartiesDbContext db)
    : ICommandHandler<DeactivateCustomFieldDefinitionCommand>
{
    public async ValueTask<Unit> Handle(DeactivateCustomFieldDefinitionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var definition = await db.CustomFieldDefinitions
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Definición de campo no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        // Baja lógica: no borra los valores ya guardados en el JSONB de los registros (quedan inertes
        // hasta que se reactive una definición con el mismo slug o se limpien).
        definition.Deactivate();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class DeleteCustomFieldDefinitionEndpoint
{
    public static RouteHandlerBuilder MapDeleteCustomFieldDefinitionEndpoint(this IEndpointRouteBuilder group) =>
        group.MapDelete(
                "/custom-fields/{id:guid}",
                async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    await mediator.Send(new DeactivateCustomFieldDefinitionCommand(id), cancellationToken);
                    return Results.NoContent();
                })
            .WithName("DeleteCustomFieldDefinition")
            .WithSummary("Deactivate a custom field definition (admin, logical delete)")
            .RequirePermission(PartiesPermissions.CustomFields.Define)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
}
