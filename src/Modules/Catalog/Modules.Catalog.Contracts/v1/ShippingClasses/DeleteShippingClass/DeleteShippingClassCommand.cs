using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ShippingClasses.DeleteShippingClass;

public sealed record DeleteShippingClassCommand(Guid Id) : ICommand<Guid>;
