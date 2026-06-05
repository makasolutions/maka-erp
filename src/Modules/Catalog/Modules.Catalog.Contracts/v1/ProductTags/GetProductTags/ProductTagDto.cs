namespace FSH.Modules.Catalog.Contracts.v1.ProductTags.GetProductTags;

public sealed record ProductTagDto(
    Guid    Id,
    Guid    ProductId,
    string  Name,
    string? Color);
