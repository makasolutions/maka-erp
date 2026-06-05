namespace FSH.Modules.Catalog.Contracts.v1.TenantProducts.GetResolvedProduct;

/// <summary>
/// A canonical product resolved against the current tenant's overrides (spec §5.7).
/// Each effective field is the tenant override when present, else the canonical value.
/// </summary>
public sealed record ResolvedProductDto(
    Guid     CanonicalProductId,
    Guid?    TenantProductId,
    bool     HasOverride,
    string   Name,
    string?  ShortDescription,
    string?  Description,
    string?  TechnicalSpecs,
    string?  SeoTitle,
    string?  SeoDescription,
    decimal? DropshippingPrice,
    decimal? DropshippingMinQty,
    bool     IsActive,
    bool     IsPublic);
