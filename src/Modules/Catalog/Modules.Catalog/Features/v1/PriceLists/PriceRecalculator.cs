using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceLists;

/// <summary>
/// Recalcula los precios de las listas derivadas (con AdjustmentPercent) a partir
/// del precio de la lista por defecto para una variación (Fase 3). Respeta los
/// ítems con IsManualOverride. No llama SaveChanges (lo hace el caller).
/// </summary>
internal static class PriceRecalculator
{
    /// <summary>
    /// Recompute the existing (non-override) items of one derived list from the
    /// default list's base prices. Used when a list's % changes.
    /// </summary>
    public static async Task RecalculateListAsync(
        CatalogDbContext db, string userId, Guid listId, decimal adjustmentPercent, CancellationToken ct)
    {
        var defaultListId = await db.PriceLists
            .Where(p => p.IsDefault && p.OwnerId == null)
            .Select(p => (Guid?)p.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);
        if (defaultListId is null) return;

        var basePrices = await db.PriceListItems
            .Where(i => i.PriceListId == defaultListId.Value)
            .Select(i => new { i.VariationId, i.Price })
            .ToDictionaryAsync(x => x.VariationId, x => x.Price, ct)
            .ConfigureAwait(false);

        var items = await db.PriceListItems
            .Where(i => i.PriceListId == listId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var item in items)
        {
            if (item.IsManualOverride) continue;
            if (!basePrices.TryGetValue(item.VariationId, out var basePrice)) continue;
            item.ApplyDerived(CatalogPricing.Derive(basePrice, adjustmentPercent), userId);
        }
    }

    public static async Task RecalculateDerivedAsync(
        CatalogDbContext db, string userId, Guid variationId, CancellationToken ct)
    {
        // Base price comes from the default list's item for this variation.
        var basePrice = await (
            from item in db.PriceListItems
            join list in db.PriceLists on item.PriceListId equals list.Id
            where list.IsDefault && list.OwnerId == null && item.VariationId == variationId
            select (decimal?)item.Price)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (basePrice is null) return;

        // Active derived Segment lists (have a %, not the default).
        var derivedLists = await db.PriceLists
            .Where(p => !p.IsDefault && p.OwnerId == null && p.IsActive
                     && p.ListKind == Contracts.Enums.PriceListKind.Segment
                     && p.AdjustmentPercent != null)
            .Select(p => new { p.Id, p.AdjustmentPercent })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (derivedLists.Count == 0) return;

        var listIds = derivedLists.Select(d => d.Id).ToList();
        var existingItems = await db.PriceListItems
            .Where(i => listIds.Contains(i.PriceListId) && i.VariationId == variationId)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var list in derivedLists)
        {
            decimal derived = CatalogPricing.Derive(basePrice.Value, list.AdjustmentPercent!.Value);
            var existing = existingItems.FirstOrDefault(i => i.PriceListId == list.Id);
            if (existing is null)
            {
                db.PriceListItems.Add(PriceListItem.Create(list.Id, variationId, derived, userId));
            }
            else
            {
                existing.ApplyDerived(derived, userId);   // no-op if manual override
            }
        }
    }
}
