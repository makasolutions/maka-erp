using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>ProductCategory — relación N:N Product↔Category — spec §2.11.</summary>
public sealed class ProductCategory : BaseEntity<Guid>
{
    public Guid ProductId  { get; private set; }
    public Guid CategoryId { get; private set; }
    public bool IsPrimary  { get; private set; }  // categoría principal del producto

    public DateTime CreatedAtUtc { get; private set; }

    private ProductCategory() { }

    public static ProductCategory Create(Guid productId, Guid categoryId, bool isPrimary = false)
        => new()
        {
            Id           = Guid.CreateVersion7(),
            ProductId    = productId,
            CategoryId   = categoryId,
            IsPrimary    = isPrimary,
            CreatedAtUtc = DateTime.UtcNow,
        };
}
