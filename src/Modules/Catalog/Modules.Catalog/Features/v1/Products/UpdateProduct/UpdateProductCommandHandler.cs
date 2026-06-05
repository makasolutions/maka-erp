using System.Text.Json;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.UpdateProduct;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.UpdateProduct;

public sealed class UpdateProductCommandHandler(CatalogDbContext db)
    : ICommandHandler<UpdateProductCommand, Guid>
{
    public async ValueTask<Guid> Handle(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var product = await db.Products
            .Where(p => !p.IsDeleted && p.Id == command.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException($"Product {command.Id} not found.");

        if (command.BrandId.HasValue)
        {
            bool brandExists = await db.Brands
                .AsNoTracking()
                .Where(b => !b.IsDeleted && b.Id == command.BrandId.Value)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!brandExists)
                throw new NotFoundException($"Brand {command.BrandId.Value} not found.");
        }

        if (command.TaxRateId.HasValue)
        {
            bool taxExists = await db.TaxRates.AsNoTracking()
                .AnyAsync(t => t.Id == command.TaxRateId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (!taxExists)
                throw new NotFoundException($"TaxRate {command.TaxRateId.Value} not found.");
        }

        if (command.ShippingClassId.HasValue)
        {
            bool shipExists = await db.ShippingClasses.AsNoTracking()
                .AnyAsync(s => s.Id == command.ShippingClassId.Value, cancellationToken)
                .ConfigureAwait(false);
            if (!shipExists)
                throw new NotFoundException($"ShippingClass {command.ShippingClassId.Value} not found.");
        }

        if (!Enum.TryParse<WeightUnit>(command.WeightUnit, ignoreCase: true, out var weightUnit))
            weightUnit = WeightUnit.KG;

        if (!Enum.TryParse<DimensionUnit>(command.DimensionUnit, ignoreCase: true, out var dimUnit))
            dimUnit = DimensionUnit.CM;

        product.UpdateDetails(
            command.Name,
            command.ShortDescription,
            command.Description,
            command.TechnicalSpecs,
            command.BrandId,
            command.TaxRateId,
            command.ShippingClassId,
            command.IsVirtual,
            command.IsDownloadable,
            command.IsPublic);

        product.UpdateShipping(
            command.Weight, weightUnit,
            command.DimensionLength, command.DimensionWidth, command.DimensionHeight, dimUnit);

        product.UpdateSeo(command.SeoTitle, command.SeoDescription, command.SeoKeywords);

        // Specs (JSONB §2.6): null/blank clears it; otherwise parse the supplied JSON.
        if (string.IsNullOrWhiteSpace(command.Specs))
        {
            product.UpdateSpecs(null);
        }
        else
        {
            // Validity is enforced by the validator; parse defensively here too.
            product.UpdateSpecs(JsonDocument.Parse(command.Specs));
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return product.Id;
    }
}
