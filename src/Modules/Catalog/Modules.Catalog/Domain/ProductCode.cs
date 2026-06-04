using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// ProductCode — tabla central de equivalencias de códigos — spec §2.9.
/// Siempre referencia una VariationId (incl. la default del Simple).
/// SupplierId null = código universal (SKU, EAN…); non-null = código de proveedor.
/// CodeType: "SKU"|"EAN"|"UPC"|"ISBN"|"GTIN"|"PartNumber"|"ManufacturerCode"|"SupplierCode" (extensible).
/// </summary>
public sealed class ProductCode : BaseEntity<Guid>
{
    public Guid    ProductId   { get; private set; }
    public Guid    VariationId { get; private set; }
    public Guid?   SupplierId  { get; private set; }  // FK futura a Suppliers
    public string  CodeType    { get; private set; } = default!;
    public string  Code        { get; private set; } = default!;
    public bool    IsPrimary   { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private ProductCode() { }

    public static ProductCode Create(
        Guid productId,
        Guid variationId,
        string codeType,
        string code,
        Guid? supplierId = null,
        bool isPrimary = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return new ProductCode
        {
            Id           = Guid.CreateVersion7(),
            ProductId    = productId,
            VariationId  = variationId,
            SupplierId   = supplierId,
            CodeType     = codeType.Trim(),
            Code         = code.Trim(),
            IsPrimary    = isPrimary,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
