using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ShippingClasses.UpdateShippingClass;

public sealed record UpdateShippingClassCommand(
    Guid    Id,
    string  Name,
    string? Description) : ICommand<Guid>;
