using FluentValidation;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Parties.Contracts.Authorization;
using FSH.Modules.Parties.Contracts.v1.CustomFields;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.CustomFields.GetCustomFieldDefinitions;

public sealed class GetCustomFieldDefinitionsQueryValidator : AbstractValidator<GetCustomFieldDefinitionsQuery>
{
    public GetCustomFieldDefinitionsQueryValidator() =>
        RuleFor(x => x.EntityType).IsInEnum().When(x => x.EntityType.HasValue);
}

public sealed class GetCustomFieldDefinitionsQueryHandler(PartiesDbContext db)
    : IQueryHandler<GetCustomFieldDefinitionsQuery, IReadOnlyList<CustomFieldDefinitionDto>>
{
    public async ValueTask<IReadOnlyList<CustomFieldDefinitionDto>> Handle(
        GetCustomFieldDefinitionsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = db.CustomFieldDefinitions.AsNoTracking();
        if (query.EntityType.HasValue) q = q.Where(d => d.EntityType == query.EntityType.Value);
        if (!query.IncludeInactive) q = q.Where(d => d.Activo);

        var list = await q
            .OrderBy(d => d.EntityType).ThenBy(d => d.Title)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return list.Select(d => d.ToDto()).ToList();
    }
}

public static class GetCustomFieldDefinitionsEndpoint
{
    public static RouteHandlerBuilder MapGetCustomFieldDefinitionsEndpoint(this IEndpointRouteBuilder group) =>
        group.MapGet(
                "/custom-fields",
                async ([AsParameters] GetCustomFieldDefinitionsQuery query, IMediator mediator, CancellationToken cancellationToken) =>
                {
                    IReadOnlyList<CustomFieldDefinitionDto> result = await mediator.Send(query, cancellationToken);
                    return Results.Ok(result);
                })
            .WithName("GetCustomFieldDefinitions")
            .WithSummary("List custom field definitions for an entity scope")
            .RequirePermission(PartiesPermissions.CustomFields.View)
            .Produces<IReadOnlyList<CustomFieldDefinitionDto>>(StatusCodes.Status200OK);
}
