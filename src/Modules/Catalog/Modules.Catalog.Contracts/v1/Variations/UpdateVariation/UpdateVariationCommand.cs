using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Variations.UpdateVariation;

public sealed record UpdateVariationCommand(
    Guid     ProductId,
    Guid     Id,
    string?  Description,
    bool     IsActive,
    decimal? Weight,
    string?  WeightUnit,
    string?  ImageUrl,
    bool     ManageStock,
    bool     AllowBackorders,
    bool     SoldIndividually,
    int?     LowStockThreshold,
    bool     IsVirtual,
    IReadOnlyList<Guid>? AttributeValueIds = null) : ICommand<Guid>;
