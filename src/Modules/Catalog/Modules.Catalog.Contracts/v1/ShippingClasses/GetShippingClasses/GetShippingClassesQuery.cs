using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ShippingClasses.GetShippingClasses;

public sealed record GetShippingClassesQuery : IQuery<IReadOnlyList<ShippingClassDto>>;
