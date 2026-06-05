using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Variations.UpdateVariation;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Variations.UpdateVariation;

public sealed class UpdateVariationCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateVariationCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateVariationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var variation = await db.Variations
            .Include(v => v.AttributeValues)
            .Where(v => !v.IsDeleted && v.ProductId == command.ProductId && v.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Variation {command.Id} not found for product {command.ProductId}.");

        // Replace the attribute-value combination when supplied (null = leave as-is).
        List<CatalogAttributeValue>? attributeValues = null;
        if (command.AttributeValueIds is not null)
        {
            attributeValues = command.AttributeValueIds.Count > 0
                ? await VariationAttributeHelper
                    .LoadAndValidateValuesAsync(db, command.AttributeValueIds, cancellationToken)
                    .ConfigureAwait(false)
                : [];

            await VariationAttributeHelper
                .EnsureCombinationUniqueAsync(db, command.ProductId, command.AttributeValueIds, command.Id, cancellationToken)
                .ConfigureAwait(false);
        }

        WeightUnit? weightUnit = null;
        if (!string.IsNullOrWhiteSpace(command.WeightUnit) &&
            Enum.TryParse<WeightUnit>(command.WeightUnit, ignoreCase: true, out var wu))
            weightUnit = wu;

        variation.Update(
            variation.Sku,   // SKU is immutable — keep existing
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

        if (attributeValues is not null)
            variation.SetAttributeValues(attributeValues);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return variation.Id;
    }
}
