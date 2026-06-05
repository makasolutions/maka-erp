using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Variations;

/// <summary>
/// Shared rules for the attribute-value combination that defines a variation
/// (spec §2.7 / §2.12). Used by AddVariation, UpdateVariation and GenerateVariations.
/// </summary>
internal static class VariationAttributeHelper
{
    /// <summary>Canonical key for a set of attribute-value ids (order-independent).</summary>
    public static string CombinationKey(IEnumerable<Guid> valueIds) =>
        string.Join('|', valueIds.OrderBy(id => id));

    /// <summary>
    /// Loads the referenced attribute values, validating they all exist and that no
    /// two of them belong to the same attribute (one value per attribute).
    /// </summary>
    public static async Task<List<CatalogAttributeValue>> LoadAndValidateValuesAsync(
        CatalogDbContext db, IReadOnlyList<Guid> valueIds, CancellationToken cancellationToken)
    {
        var distinctIds = valueIds.Distinct().ToList();

        var values = await db.Set<CatalogAttributeValue>()
            .Where(v => distinctIds.Contains(v.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (values.Count != distinctIds.Count)
            throw new CustomException(
                "Uno o más valores de atributo no existen.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        bool duplicateAttribute = values
            .GroupBy(v => v.AttributeId)
            .Any(g => g.Count() > 1);

        if (duplicateAttribute)
            throw new CustomException(
                "Una variación no puede tener dos valores del mismo atributo.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        return values;
    }

    /// <summary>
    /// Ensures no other (non-deleted) variation of the product already uses the same
    /// attribute-value combination. Pass <paramref name="excludeVariationId"/> when updating.
    /// </summary>
    public static async Task EnsureCombinationUniqueAsync(
        CatalogDbContext db, Guid productId, IReadOnlyList<Guid> valueIds,
        Guid? excludeVariationId, CancellationToken cancellationToken)
    {
        string targetKey = CombinationKey(valueIds);

        var existing = await db.Variations
            .AsNoTracking()
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .Where(v => excludeVariationId == null || v.Id != excludeVariationId)
            .Select(v => new
            {
                v.Id,
                ValueIds = v.AttributeValues.Select(av => av.Id).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        bool collision = existing.Exists(e => CombinationKey(e.ValueIds) == targetKey);
        if (collision)
            throw new CustomException(
                "Ya existe una variación con esa combinación de atributos.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);
    }
}
