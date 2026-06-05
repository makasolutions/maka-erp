using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttribute;

public sealed record UpdateAttributeCommand(
    Guid                 Id,
    string               Name,
    CatalogAttributeType Type,
    bool                 IsVisibleOnProduct,
    bool                 IsUsedForVariations,
    int                  SortOrder) : ICommand<Guid>;
