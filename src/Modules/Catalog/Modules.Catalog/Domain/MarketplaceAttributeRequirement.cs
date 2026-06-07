using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// MarketplaceAttributeRequirement — atributo exigido por un marketplace
/// (Google/Mercado Libre) para una categoría (Fase 2 §RF). Conduce la validación
/// no bloqueante del producto y el reporte de cobertura.
/// </summary>
public sealed class MarketplaceAttributeRequirement : BaseEntity<Guid>
{
    public Marketplace Marketplace { get; private set; }
    public Guid        CategoryId  { get; private set; }
    public Guid        AttributeId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    private MarketplaceAttributeRequirement() { }

    public static MarketplaceAttributeRequirement Create(Marketplace marketplace, Guid categoryId, Guid attributeId)
        => new()
        {
            Id           = Guid.CreateVersion7(),
            Marketplace  = marketplace,
            CategoryId   = categoryId,
            AttributeId  = attributeId,
            CreatedAtUtc = DateTime.UtcNow,
        };
}
