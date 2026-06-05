using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Variations.GenerateVariations;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Extensions;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Variations.GenerateVariations;

public sealed class GenerateVariationsCommandHandler(CatalogDbContext db)
    : ICommandHandler<GenerateVariationsCommand, GenerateVariationsResult>
{
    public async ValueTask<GenerateVariationsResult> Handle(GenerateVariationsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.Id == command.ProductId)
            .Select(p => new { p.Id, p.Slug })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        // Attributes that drive variations, with their selected values, in order.
        var variationAttributes = await db.Set<ProductAttribute>()
            .Where(pa => pa.ProductId == command.ProductId && pa.IsUsedForVariations)
            .Include(pa => pa.SelectedValues)
            .OrderBy(pa => pa.SortOrder)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (variationAttributes.Count == 0)
            throw new CustomException(
                "El producto no tiene atributos marcados para variaciones.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        if (variationAttributes.Exists(pa => pa.SelectedValues.Count == 0))
            throw new CustomException(
                "Todo atributo de variación debe tener al menos un valor seleccionado.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        // Cartesian product of the selected values across attributes.
        var valueLists = variationAttributes
            .Select(pa => pa.SelectedValues.OrderBy(v => v.SortOrder).ThenBy(v => v.Value).ToList())
            .ToList();

        var combinations = CartesianProduct(valueLists);

        // Existing combinations (by canonical key) to skip.
        var existing = await db.Variations
            .AsNoTracking()
            .Where(v => v.ProductId == command.ProductId && !v.IsDeleted)
            .Select(v => v.AttributeValues.Select(av => av.Id).ToList())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existingKeys = existing
            .Select(VariationAttributeHelper.CombinationKey)
            .ToHashSet(StringComparer.Ordinal);

        bool hasDefault = await db.Variations
            .AsNoTracking()
            .AnyAsync(v => v.ProductId == command.ProductId && !v.IsDeleted && v.IsDefault, cancellationToken)
            .ConfigureAwait(false);

        // Track SKUs we have used (DB + this batch) to keep them globally unique.
        var usedSkus = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int created = 0;
        int skipped = 0;
        var createdIds = new List<Guid>();

        foreach (var combination in combinations)
        {
            string key = VariationAttributeHelper.CombinationKey(combination.Select(v => v.Id));
            if (!existingKeys.Add(key))
            {
                skipped++;
                continue;
            }

            string sku = await BuildUniqueSkuAsync(product.Slug, combination, usedSkus, cancellationToken)
                .ConfigureAwait(false);

            var variation = ProductVariation.Create(product.Id, sku, isDefault: !hasDefault);
            hasDefault = true;   // only the first generated one can claim default
            variation.SetAttributeValues(combination);

            db.Variations.Add(variation);
            createdIds.Add(variation.Id);
            created++;
        }

        if (created > 0)
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new GenerateVariationsResult(created, skipped, createdIds);
    }

    private async Task<string> BuildUniqueSkuAsync(
        string productSlug, List<CatalogAttributeValue> combination,
        HashSet<string> usedSkus, CancellationToken cancellationToken)
    {
        string suffix = string.Join('-', combination.Select(v => SlugHelper.Build(null, v.Value)));
        string baseSku = $"{productSlug}-{suffix}".ToUpperInvariant();

        string candidate = baseSku;
        int counter = 1;
        while (usedSkus.Contains(candidate) || await SkuExistsAsync(candidate, cancellationToken).ConfigureAwait(false))
        {
            counter++;
            candidate = $"{baseSku}-{counter}";
        }

        usedSkus.Add(candidate);
        return candidate;
    }

    private Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken) =>
        db.Variations
            .IgnoreQueryFilters()
            .Where(v => v.Sku == sku)
            .AnyAsync(cancellationToken);

    private static List<List<CatalogAttributeValue>> CartesianProduct(List<List<CatalogAttributeValue>> lists)
    {
        var result = new List<List<CatalogAttributeValue>> { new() };

        foreach (var list in lists)
        {
            var next = new List<List<CatalogAttributeValue>>(result.Count * list.Count);
            foreach (var partial in result)
            {
                foreach (var value in list)
                {
                    var combo = new List<CatalogAttributeValue>(partial) { value };
                    next.Add(combo);
                }
            }
            result = next;
        }

        return result;
    }
}
