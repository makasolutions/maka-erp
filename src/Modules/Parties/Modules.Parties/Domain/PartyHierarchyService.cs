using FSH.Modules.Parties.Data;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Carga la cadena de ancestros de un tercero para alimentar las validaciones de jerarquía del
/// dominio (PR-D3). Patrón de referencia para otras jerarquías del sistema (categorías de catálogo,
/// centros de costo, etc.): el dominio valida en memoria; este servicio carga la cadena con UN
/// recursive CTE (solo ids para ciclos; entidades para la delegación), acotado en profundidad.
/// </summary>
public sealed class PartyHierarchyService(PartiesDbContext db)
{
    /// <summary>Tope de profundidad — guardarraíl anti-corrupción (jerarquías reales son de pocos niveles).</summary>
    public const int MaxDepth = 50;

    /// <summary>
    /// Ids de los ancestros de <paramref name="partyId"/> (padre, abuelo, …), vía recursive CTE.
    /// Una sola query, solo ids — para validar ciclos sin cargar entidades. NO incluye a <paramref name="partyId"/>.
    /// </summary>
    public async Task<IReadOnlySet<Guid>> GetAncestorIdsAsync(Guid partyId, CancellationToken ct = default)
    {
        const string sql = """
            WITH RECURSIVE ancestors AS (
                SELECT p."ParentPartyId" AS id, 1 AS depth
                FROM parties."Parties" p
                WHERE p."Id" = {0} AND p."ParentPartyId" IS NOT NULL
                UNION ALL
                SELECT p."ParentPartyId", a.depth + 1
                FROM parties."Parties" p
                JOIN ancestors a ON p."Id" = a.id
                WHERE p."ParentPartyId" IS NOT NULL AND a.depth < {1}
            )
            SELECT id AS "Value" FROM ancestors
            """;

        var ids = await db.Database
            .SqlQueryRaw<Guid>(sql, partyId, MaxDepth)
            .ToListAsync(ct).ConfigureAwait(false);

        return ids.ToHashSet();
    }

    /// <summary>
    /// Carga los ancestros de <paramref name="partyId"/> como entidades, del más cercano al más lejano,
    /// para <see cref="Party.ResolveCommercialEntity"/>. Carga iterativa acotada (las jerarquías son
    /// cortas: matriz→sucursal→contacto). Incluye <c>FiscalData</c> (owned, ya viene en la fila).
    /// </summary>
    public async Task<IReadOnlyList<Party>> GetAncestorsNearestFirstAsync(Guid partyId, CancellationToken ct = default)
    {
        var result = new List<Party>();
        var current = await db.Parties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == partyId, ct).ConfigureAwait(false);
        var depth = 0;
        while (current?.ParentPartyId is { } parentId && depth < MaxDepth)
        {
            var parent = await db.Parties.AsNoTracking().FirstOrDefaultAsync(p => p.Id == parentId, ct).ConfigureAwait(false);
            if (parent is null) break;
            result.Add(parent);
            current = parent;
            depth++;
        }
        return result;
    }
}
