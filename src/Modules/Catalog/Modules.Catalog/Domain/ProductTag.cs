using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>ProductTag — spec §2.10. Ej: "Nuevo", "Oferta", "Exclusivo".</summary>
public sealed class ProductTag : BaseEntity<Guid>
{
    public Guid    ProductId { get; private set; }
    public string  Name      { get; private set; } = default!;
    public string? Color     { get; private set; }  // hex para la etiqueta en UI

    public DateTime CreatedAtUtc { get; private set; }

    private ProductTag() { }

    public static ProductTag Create(Guid productId, string name, string? color = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ProductTag
        {
            Id           = Guid.CreateVersion7(),
            ProductId    = productId,
            Name         = name.Trim(),
            Color        = color,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
