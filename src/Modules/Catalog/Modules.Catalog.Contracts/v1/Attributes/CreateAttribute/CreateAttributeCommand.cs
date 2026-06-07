using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Attributes.CreateAttribute;

public sealed record CreateAttributeCommand(
    string               Name,
    string?              Slug,
    CatalogAttributeType Type,
    bool                 IsVisibleOnProduct  = true,
    bool                 IsUsedForVariations = false,
    int                  SortOrder           = 0,
    IReadOnlyList<Guid>? CategoryIds         = null) : ICommand<Guid>;
