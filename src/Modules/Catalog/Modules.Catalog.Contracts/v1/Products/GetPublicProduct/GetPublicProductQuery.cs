using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Products.GetPublicProduct;

public sealed record GetPublicProductQuery(string Slug) : IQuery<PublicProductDto>;
