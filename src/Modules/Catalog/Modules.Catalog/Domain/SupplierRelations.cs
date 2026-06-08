using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// SupplierBrand — spec §2.16. Esquema creado ahora para no migrar después;
/// la lógica de proveedores se implementa en el módulo Purchasing.
/// SupplierId es FK futura a Suppliers (módulo Purchasing).
/// </summary>
public sealed class SupplierBrand : BaseEntity<Guid>
{
    public Guid    SupplierId             { get; private set; }
    public Guid    BrandId                { get; private set; }
    public bool    IsExclusiveDistributor { get; private set; }  // ej: DZOFilm Colombia
    public string? Notes                  { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private SupplierBrand() { }

    public static SupplierBrand Create(Guid supplierId, Guid brandId, bool isExclusiveDistributor = false, string? notes = null)
        => new()
        {
            Id                     = Guid.CreateVersion7(),
            SupplierId             = supplierId,
            BrandId                = brandId,
            IsExclusiveDistributor = isExclusiveDistributor,
            Notes                  = notes,
            CreatedAtUtc           = DateTime.UtcNow,
        };
}

/// <summary>
/// SupplierCategory — categoría de la taxonomía global que un proveedor
/// comercializa. <c>CategoryId</c> referencia una categoría del tenant `global`.
/// </summary>
public sealed class SupplierCategory : BaseEntity<Guid>
{
    public Guid SupplierId { get; private set; }
    public Guid CategoryId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private SupplierCategory() { }

    public static SupplierCategory Create(Guid supplierId, Guid categoryId) => new()
    {
        Id = Guid.CreateVersion7(),
        SupplierId = supplierId,
        CategoryId = categoryId,
        CreatedAtUtc = DateTime.UtcNow,
    };
}

/// <summary>
/// SupplierProduct — spec §2.16. El código del proveedor vive en
/// ProductCode (CodeType="SupplierCode", SupplierId=X).
/// </summary>
public sealed class SupplierProduct : BaseEntity<Guid>
{
    public Guid      SupplierId          { get; private set; }
    public Guid      ProductId           { get; private set; }
    public Guid      VariationId         { get; private set; }
    public decimal?  CostPrice           { get; private set; }
    public string?   CostCurrency        { get; private set; }  // "USD"|"COP"|"EUR"
    public bool      IsPreferredSupplier { get; private set; }
    public DateTime? LastPriceUpdate     { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private SupplierProduct() { }

    public static SupplierProduct Create(
        Guid supplierId,
        Guid productId,
        Guid variationId,
        decimal? costPrice = null,
        string? costCurrency = null,
        bool isPreferredSupplier = false)
        => new()
        {
            Id                  = Guid.CreateVersion7(),
            SupplierId          = supplierId,
            ProductId           = productId,
            VariationId         = variationId,
            CostPrice           = costPrice,
            CostCurrency        = costCurrency,
            IsPreferredSupplier = isPreferredSupplier,
            CreatedAtUtc        = DateTime.UtcNow,
        };

    public void RecordPriceUpdate(decimal? costPrice, string? costCurrency)
    {
        CostPrice       = costPrice;
        CostCurrency    = costCurrency;
        LastPriceUpdate = DateTime.UtcNow;
    }
}
