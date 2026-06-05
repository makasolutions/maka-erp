using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ShippingClasses.CreateShippingClass;

public sealed record CreateShippingClassCommand(
    string  Name,
    string? Description = null) : ICommand<Guid>;
