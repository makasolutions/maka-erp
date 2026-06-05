using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Variations.AddVariation;

public sealed record AddVariationCommand(
    Guid     ProductId,
    string   Sku,
    string?  Description    = null,
    bool     IsDefault      = false,
    bool     IsActive       = true,
    decimal? Weight         = null,
    string?  WeightUnit     = null,
    string?  ImageUrl       = null,
    bool     ManageStock    = true,
    bool     AllowBackorders    = false,
    bool     SoldIndividually   = false,
    int?     LowStockThreshold  = null,
    bool     IsVirtual          = false,
    IReadOnlyList<Guid>? AttributeValueIds = null) : ICommand<Guid>;
