using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PartyPriceLists;

public sealed record PartyPriceListDto(
    Guid          Id,
    Guid          PartyId,
    Guid          PriceListId,
    string        PriceListName,
    PriceListKind ListKind,
    bool          IsActive,
    DateTime?     ValidFrom,
    DateTime?     ValidTo);

/// <summary>Listas de precios asignadas a un tercero.</summary>
public sealed record GetPartyPriceListsQuery(Guid PartyId) : IQuery<IReadOnlyList<PartyPriceListDto>>;

/// <summary>Asigna una lista de precios a un tercero.</summary>
public sealed record AssignPartyPriceListCommand(
    Guid PartyId, Guid PriceListId, DateTime? ValidFrom = null, DateTime? ValidTo = null) : ICommand<Guid>;

/// <summary>Quita una asignación de lista de precios.</summary>
public sealed record RemovePartyPriceListCommand(Guid Id) : ICommand;
