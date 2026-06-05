using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttributeValue;

public sealed record UpdateAttributeValueCommand(
    Guid    AttributeId,
    Guid    ValueId,
    string  Value,
    string? ColorCode = null,
    string? ImageUrl  = null,
    int     SortOrder = 0) : ICommand<Guid>;
