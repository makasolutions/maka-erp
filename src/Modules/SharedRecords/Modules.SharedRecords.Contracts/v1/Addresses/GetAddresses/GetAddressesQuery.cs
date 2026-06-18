using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Addresses.GetAddresses;

/// <summary>Lista las direcciones de un owner (scoped). Bounded — sin paginación.</summary>
public sealed record GetAddressesQuery(string OwnerType, Guid OwnerId)
    : IQuery<IReadOnlyList<AddressDto>>;
