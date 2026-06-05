using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductAttributes;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.SetProductAttributes;

public sealed class SetProductAttributesCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetProductAttributesCommand, Guid>
{
    public async ValueTask<Guid> Handle(SetProductAttributesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool productExists = await db.Products
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (!productExists)
            throw new NotFoundException($"Product {command.ProductId} not found.");

        // Dedupe assignments by AttributeId.
        var desired = command.Attributes
            .GroupBy(a => a.AttributeId)
            .Select(g => g.First())
            .ToList();

        // Validate every referenced attribute exists.
        var attributeIds = desired.Select(d => d.AttributeId).ToList();
        if (attributeIds.Count > 0)
        {
            int foundAttributes = await db.Attributes
                .AsNoTracking()
                .CountAsync(a => attributeIds.Contains(a.Id), cancellationToken)
                .ConfigureAwait(false);

            if (foundAttributes != attributeIds.Count)
                throw new CustomException(
                    "Uno o más atributos no existen.",
                    Enumerable.Empty<string>(),
                    HttpStatusCode.BadRequest);
        }

        // Load all referenced values (tracked) and validate each belongs to its attribute.
        var allValueIds = desired.SelectMany(d => d.ValueIds).Distinct().ToList();
        var values = allValueIds.Count > 0
            ? await db.Set<CatalogAttributeValue>()
                .Where(v => allValueIds.Contains(v.Id))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false)
            : [];

        if (values.Count != allValueIds.Count)
            throw new CustomException(
                "Uno o más valores de atributo no existen.",
                Enumerable.Empty<string>(),
                HttpStatusCode.BadRequest);

        var valuesById = values.ToDictionary(v => v.Id);
        foreach (var assignment in desired)
        {
            foreach (var valueId in assignment.ValueIds)
            {
                if (valuesById[valueId].AttributeId != assignment.AttributeId)
                    throw new CustomException(
                        $"El valor {valueId} no pertenece al atributo {assignment.AttributeId}.",
                        Enumerable.Empty<string>(),
                        HttpStatusCode.BadRequest);
            }
        }

        // Replace the full set: remove existing assignments (join rows cascade), then add fresh.
        var existing = await db.Set<ProductAttribute>()
            .Include(pa => pa.SelectedValues)
            .Where(pa => pa.ProductId == command.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing.Count > 0)
            db.Set<ProductAttribute>().RemoveRange(existing);

        foreach (var assignment in desired)
        {
            var productAttribute = ProductAttribute.Create(
                command.ProductId,
                assignment.AttributeId,
                assignment.IsUsedForVariations,
                assignment.IsVisibleOnProduct,
                assignment.SortOrder);

            productAttribute.SetSelectedValues(assignment.ValueIds.Select(id => valuesById[id]));
            db.Set<ProductAttribute>().Add(productAttribute);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return command.ProductId;
    }
}
