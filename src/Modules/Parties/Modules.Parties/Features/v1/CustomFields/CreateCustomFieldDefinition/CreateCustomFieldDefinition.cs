using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.CustomFields;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain.CustomFields;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.CustomFields.CreateCustomFieldDefinition;

public sealed class CreateCustomFieldDefinitionCommandValidator : AbstractValidator<CreateCustomFieldDefinitionCommand>
{
    public CreateCustomFieldDefinitionCommandValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.FieldType).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.DefaultValue).MaximumLength(1024);
        RuleFor(x => x.Options)
            .NotEmpty()
            .When(x => x.FieldType is CustomFieldType.Select or CustomFieldType.MultiSelect)
            .WithMessage("Los campos Select/MultiSelect requieren al menos una opción.");
    }
}

public sealed class CreateCustomFieldDefinitionCommandHandler(PartiesDbContext db)
    : ICommandHandler<CreateCustomFieldDefinitionCommand, Guid>
{
    public async ValueTask<Guid> Handle(CreateCustomFieldDefinitionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        string slug = CustomFieldDefinition.NormalizeSlug(
            string.IsNullOrWhiteSpace(command.ApiSlug) ? command.Title : command.ApiSlug);

        // Pre-chequeo amigable (el índice parcial único ix_customfielddef_slug es la garantía real).
        bool exists = await db.CustomFieldDefinitions
            .AsNoTracking()
            .AnyAsync(d => d.EntityType == command.EntityType && d.ApiSlug == slug && d.Activo, cancellationToken)
            .ConfigureAwait(false);
        if (exists)
            throw new CustomException($"Ya existe un campo activo con el slug '{slug}' para ese alcance.",
                Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var definition = CustomFieldDefinition.Create(
            command.EntityType, command.Title, command.ApiSlug, command.FieldType, command.Description,
            command.IsRequired, command.IsUnique, command.IsDefaultValueEnabled, command.DefaultValue,
            command.IsMultiselect, command.Options.ToDomain());

        db.CustomFieldDefinitions.Add(definition);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return definition.Id;
    }
}

public static class CreateCustomFieldDefinitionEndpoint
{
    public static RouteHandlerBuilder MapCreateCustomFieldDefinitionEndpoint(this IEndpointRouteBuilder group) =>
        group.MapPost(
                "/custom-fields",
                async (CreateCustomFieldDefinitionCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    Guid id = await mediator.Send(command, cancellationToken);
                    return Results.Created($"/api/v1/parties/custom-fields/{id}", id);
                })
            .WithName("CreateCustomFieldDefinition")
            .WithSummary("Define a custom field (schema change — admin)")
            .RequirePermission(PartiesPermissions.CustomFields.Define)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status409Conflict);
}
