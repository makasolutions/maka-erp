using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Phones.GetPhones;

/// <summary>Lista los teléfonos de un owner (scoped). Bounded — sin paginación.</summary>
public sealed record GetPhonesQuery(string OwnerType, Guid OwnerId)
    : IQuery<IReadOnlyList<PhoneDto>>;
