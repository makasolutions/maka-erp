using FSH.Framework.Core.Domain;
using FSH.Modules.Catalog.Contracts.Enums;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Alias/sinónimo de un objeto del catálogo global (categoría o marca) para la búsqueda
/// inteligente. Vive en el tenant `global` (compartido). Ej.: alias "cannon" → marca Canon,
/// "celular" → categoría Teléfonos. La búsqueda combina similitud trigram sobre el nombre con
/// coincidencia trigram sobre estos alias.
/// </summary>
public sealed class CatalogAlias : BaseEntity<Guid>
{
    public CatalogAliasEntity EntityType { get; private set; }
    public Guid               TargetId   { get; private set; }
    public string             Alias      { get; private set; } = default!;

    public DateTime CreatedAtUtc { get; private set; }

    private CatalogAlias() { }

    public static CatalogAlias Create(CatalogAliasEntity entityType, Guid targetId, string alias)
    {
        if (targetId == Guid.Empty) throw new ArgumentException("TargetId requerido.", nameof(targetId));
        ArgumentException.ThrowIfNullOrWhiteSpace(alias);
        return new CatalogAlias
        {
            Id           = Guid.CreateVersion7(),
            EntityType   = entityType,
            TargetId     = targetId,
            Alias        = alias.Trim(),
            CreatedAtUtc = DateTime.UtcNow,
        };
    }
}
