using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// CategoryAttribute — relación N:N entre <see cref="Category"/> y
/// <see cref="CatalogAttribute"/>. Define qué atributos "esperan" los productos de
/// una categoría (plantilla): el formulario del producto precarga/filtra por estos,
/// pero el producto puede añadir cualquier otro (override libre).
/// </summary>
public sealed class CategoryAttribute : BaseEntity<Guid>
{
    public Guid CategoryId  { get; private set; }
    public Guid AttributeId { get; private set; }
    public int  SortOrder   { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private CategoryAttribute() { }

    public static CategoryAttribute Create(Guid categoryId, Guid attributeId, int sortOrder = 0)
        => new()
        {
            Id           = Guid.CreateVersion7(),
            CategoryId   = categoryId,
            AttributeId  = attributeId,
            SortOrder    = sortOrder,
            CreatedAtUtc = DateTime.UtcNow,
        };
}
