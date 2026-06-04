using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Variations.UpdateVariation;
using FSH.Modules.Catalog.Data;
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
            .Where(v => !v.IsDeleted && v.ProductId == command.ProductId && v.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Variation {command.Id} not found for product {command.ProductId}.");

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

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return variation.Id;
    }
}
