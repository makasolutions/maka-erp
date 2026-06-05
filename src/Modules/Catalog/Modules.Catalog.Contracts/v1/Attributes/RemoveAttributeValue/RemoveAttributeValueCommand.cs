using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.RemoveAttributeValue;

public sealed record RemoveAttributeValueCommand(
    Guid AttributeId,
    Guid ValueId) : ICommand<Guid>;
