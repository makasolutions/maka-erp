using System.Net;
using FluentValidation;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Identity.Authorization;
using FSH.Modules.Catalog.Contracts.Authorization;
using FSH.Modules.Catalog.Contracts.v1.PartyPriceLists;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PartyPriceLists;

public sealed class GetPartyPriceListsQueryHandler(CatalogDbContext db)
    : IQueryHandler<GetPartyPriceListsQuery, IReadOnlyList<PartyPriceListDto>>
{
    public async ValueTask<IReadOnlyList<PartyPriceListDto>> Handle(GetPartyPriceListsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return await (from ppl in db.PartyPriceLists.AsNoTracking()
                      join pl in db.PriceLists.AsNoTracking() on ppl.PriceListId equals pl.Id
                      where ppl.PartyId == query.PartyId
                      orderby pl.Name
                      select new PartyPriceListDto(
                          ppl.Id, ppl.PartyId, ppl.PriceListId, pl.Name, pl.ListKind,
                          ppl.IsActive, ppl.ValidFrom, ppl.ValidTo))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}

public sealed class AssignPartyPriceListCommandValidator : AbstractValidator<AssignPartyPriceListCommand>
{
    public AssignPartyPriceListCommandValidator()
    {
        RuleFor(x => x.PartyId).NotEmpty();
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.ValidTo).GreaterThanOrEqualTo(x => x.ValidFrom)
            .When(x => x.ValidFrom.HasValue && x.ValidTo.HasValue);
    }
}

public sealed class AssignPartyPriceListCommandHandler(CatalogDbContext db)
    : ICommandHandler<AssignPartyPriceListCommand, Guid>
{
    public async ValueTask<Guid> Handle(AssignPartyPriceListCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool listExists = await db.PriceLists.AsNoTracking()
            .AnyAsync(p => p.Id == command.PriceListId, cancellationToken).ConfigureAwait(false);
        if (!listExists)
            throw new CustomException("Lista de precios no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        bool dup = await db.PartyPriceLists.AsNoTracking()
            .AnyAsync(x => x.PartyId == command.PartyId && x.PriceListId == command.PriceListId, cancellationToken)
            .ConfigureAwait(false);
        if (dup)
            throw new CustomException("Esa lista ya está asignada a este tercero.", Enumerable.Empty<string>(), HttpStatusCode.Conflict);

        var entity = PartyPriceList.Create(command.PartyId, command.PriceListId, command.ValidFrom, command.ValidTo);
        db.PartyPriceLists.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return entity.Id;
    }
}

public sealed class RemovePartyPriceListCommandValidator : AbstractValidator<RemovePartyPriceListCommand>
{
    public RemovePartyPriceListCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}

public sealed class RemovePartyPriceListCommandHandler(CatalogDbContext db)
    : ICommandHandler<RemovePartyPriceListCommand>
{
    public async ValueTask<Unit> Handle(RemovePartyPriceListCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var entity = await db.PartyPriceLists.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Asignación no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        db.PartyPriceLists.Remove(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}

public static class PartyPriceListEndpoints
{
    public static void MapPartyPriceListEndpoints(this IEndpointRouteBuilder group)
    {
        group.MapGet("/{partyId:guid}",
                async (Guid partyId, IMediator m, CancellationToken ct) =>
                    Results.Ok(await m.Send(new GetPartyPriceListsQuery(partyId), ct)))
            .WithName("GetPartyPriceLists").WithSummary("Price lists assigned to a party")
            .RequirePermission(CatalogPermissions.PriceLists.View)
            .Produces<IReadOnlyList<PartyPriceListDto>>(StatusCodes.Status200OK);

        group.MapPost("/",
                async (AssignPartyPriceListCommand cmd, IMediator m, CancellationToken ct) =>
                {
                    Guid id = await m.Send(cmd, ct);
                    return TypedResults.Created($"/api/v1/catalog/party-price-lists/{id}", id);
                })
            .WithName("AssignPartyPriceList").WithSummary("Assign a price list to a party")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}",
                async (Guid id, IMediator m, CancellationToken ct) =>
                {
                    await m.Send(new RemovePartyPriceListCommand(id), ct);
                    return Results.NoContent();
                })
            .WithName("RemovePartyPriceList").WithSummary("Remove a party price-list assignment")
            .RequirePermission(CatalogPermissions.PriceLists.Manage)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }
}
