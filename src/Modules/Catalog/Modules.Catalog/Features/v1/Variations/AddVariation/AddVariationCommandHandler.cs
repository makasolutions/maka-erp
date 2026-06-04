using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Variations.AddVariation;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Variations.AddVariation;

public sealed class AddVariationCommandHandler(CatalogDbContext db)
    : ICommandHandler<AddVariationCommand, Guid>
{
    public async ValueTask<Guid> Handle(AddVariationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .Where(p => !p.IsDeleted && p.Id == command.ProductId)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.ProductId} not found.");

        // SKU unique globally — check across all tenants and soft-deleted rows
        string normalizedSku = command.Sku.Trim().ToUpperInvariant();

        bool skuExists = await db.Variations
            .IgnoreQueryFilters()
            .Where(v => v.Sku == normalizedSku)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        if (skuExists)
            throw new CustomException(
                $"El SKU '{normalizedSku}' ya existe.",
                Enumerable.Empty<string>(),
                HttpStatusCode.Conflict);

        // If new variation is default, clear IsDefault from the current default variation
        if (command.IsDefault)
        {
            var currentDefault = await db.Variations
                .Where(v => v.ProductId == command.ProductId && v.IsDefault && !v.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            currentDefault?.ClearDefault();
        }

        WeightUnit? weightUnit = null;
        if (!string.IsNullOrWhiteSpace(command.WeightUnit) &&
            Enum.TryParse<WeightUnit>(command.WeightUnit, ignoreCase: true, out var wu))
            weightUnit = wu;

        var variation = ProductVariation.Create(
            product.Id,
            normalizedSku,
            command.Description,
            command.IsDefault);

        variation.Update(
            normalizedSku,
            command.Description,
            command.IsActive,
            command.Weight,
            weightUnit,
            null, null, null,
            command.ImageUrl,
            command.ManageStock,
            command.AllowBackorders,
            command.SoldIndividually,
            command.LowStockThreshold,
            command.IsVirtual);

        db.Variations.Add(variation);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return variation.Id;
    }
}
