using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.AddAttributeValue;

public sealed record AddAttributeValueCommand(
    Guid    AttributeId,
    string  Value,
    string? ColorCode = null,
    string? ImageUrl  = null,
    int     SortOrder = 0) : ICommand<Guid>;
