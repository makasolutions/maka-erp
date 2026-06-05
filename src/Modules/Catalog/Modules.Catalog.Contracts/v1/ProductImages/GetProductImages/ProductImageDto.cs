namespace FSH.Modules.Catalog.Contracts.v1.ProductImages.GetProductImages;

public sealed record ProductImageDto(
    Guid    Id,
    Guid    ProductId,
    string  Url,
    string? AltText,
    bool    IsPrimary,
    int     SortOrder);
