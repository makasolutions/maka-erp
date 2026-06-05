using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.ProductImages.GetProductImages;

public sealed record GetProductImagesQuery(Guid ProductId) : IQuery<IReadOnlyList<ProductImageDto>>;
