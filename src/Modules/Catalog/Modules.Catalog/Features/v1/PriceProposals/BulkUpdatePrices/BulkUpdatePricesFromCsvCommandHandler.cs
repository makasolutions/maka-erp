using System.Globalization;
using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.BulkUpdatePrices;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.BulkUpdatePrices;

public sealed class BulkUpdatePricesFromCsvCommandHandler(CatalogDbContext db, ICurrentUser currentUser)
    : ICommandHandler<BulkUpdatePricesFromCsvCommand, BulkUpdatePricesResult>
{
    public async ValueTask<BulkUpdatePricesResult> Handle(BulkUpdatePricesFromCsvCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool listExists = await db.PriceLists
            .AsNoTracking()
            .AnyAsync(p => p.Id == command.PriceListId, cancellationToken)
            .ConfigureAwait(false);

        if (!listExists)
            throw new NotFoundException($"Price list {command.PriceListId} not found.");

        // Parse CSV → (supplierCode, newPrice) rows.
        var rows = ParseCsv(command.CsvContent, command.SupplierCodeColumn, command.PriceColumn);

        // Supplier codes for this supplier → VariationId (single round-trip).
        var supplierCodeMap = await db.ProductCodes
            .AsNoTracking()
            .Where(c => c.SupplierId == command.SupplierId && c.CodeType == "SupplierCode")
            .Select(c => new { c.Code, c.VariationId })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var codeToVariation = supplierCodeMap
            .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().VariationId, StringComparer.OrdinalIgnoreCase);

        // Existing items in the target list → current price (to populate OldPrice).
        var existingItems = await db.PriceListItems
            .AsNoTracking()
            .Where(i => i.PriceListId == command.PriceListId)
            .Select(i => new { i.Id, i.VariationId, i.Price })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var variationToItem = existingItems.ToDictionary(i => i.VariationId, i => (i.Id, i.Price));

        var batchId = Guid.CreateVersion7();
        string userId = currentUser.GetUserId().ToString();
        var notFound = new List<string>();
        var proposals = new List<PriceBulkProposal>();

        foreach (var (supplierCode, newPrice) in rows)
        {
            if (!codeToVariation.TryGetValue(supplierCode, out var variationId))
            {
                notFound.Add(supplierCode);
                continue;
            }

            Guid?    itemId   = null;
            decimal? oldPrice = null;
            if (variationToItem.TryGetValue(variationId, out var existing))
            {
                itemId   = existing.Id;
                oldPrice = existing.Price;
            }

            proposals.Add(PriceBulkProposal.Create(
                batchId,
                command.PriceListId,
                variationId,
                command.SupplierId,
                supplierCode,
                newPrice,
                userId,
                itemId,
                oldPrice,
                command.ChangeReason,
                command.SourceReference));
        }

        if (proposals.Count > 0)
        {
            db.PriceBulkProposals.AddRange(proposals);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new BulkUpdatePricesResult(
            batchId,
            MatchedCount: proposals.Count,
            NotFoundCount: notFound.Count,
            ProposedCount: proposals.Count,
            NotFoundCodes: notFound);
    }

    /// <summary>
    /// Minimal CSV parse: first non-empty line = header; locate the two columns by
    /// name (case-insensitive). Does NOT handle quoted commas — supplier price
    /// lists are plain 2-column files. Malformed/unparseable rows are skipped.
    /// </summary>
    private static List<(string SupplierCode, decimal NewPrice)> ParseCsv(
        string csv, string supplierCodeColumn, string priceColumn)
    {
        var result = new List<(string, decimal)>();
        if (string.IsNullOrWhiteSpace(csv)) return result;

        var lines = csv.Replace("\r\n", "\n", StringComparison.Ordinal)
                       .Replace('\r', '\n')
                       .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2) return result;

        var header = lines[0].Split(',', StringSplitOptions.TrimEntries);
        int codeIdx  = Array.FindIndex(header, h => string.Equals(h, supplierCodeColumn, StringComparison.OrdinalIgnoreCase));
        int priceIdx = Array.FindIndex(header, h => string.Equals(h, priceColumn, StringComparison.OrdinalIgnoreCase));

        if (codeIdx < 0 || priceIdx < 0)
            throw new CustomException(
                $"El CSV no contiene las columnas '{supplierCodeColumn}' y/o '{priceColumn}'.",
                Enumerable.Empty<string>(),
                System.Net.HttpStatusCode.BadRequest);

        for (int i = 1; i < lines.Length; i++)
        {
            var cells = lines[i].Split(',', StringSplitOptions.TrimEntries);
            if (cells.Length <= Math.Max(codeIdx, priceIdx)) continue;

            string code = cells[codeIdx];
            if (string.IsNullOrWhiteSpace(code)) continue;

            if (!decimal.TryParse(cells[priceIdx], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
                continue;
            if (price < 0) continue;

            result.Add((code, price));
        }

        return result;
    }
}
