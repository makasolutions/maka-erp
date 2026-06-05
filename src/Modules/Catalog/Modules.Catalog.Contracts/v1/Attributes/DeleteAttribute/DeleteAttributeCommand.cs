using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.DeleteAttribute;

public sealed record DeleteAttributeCommand(Guid Id) : ICommand<Guid>;
