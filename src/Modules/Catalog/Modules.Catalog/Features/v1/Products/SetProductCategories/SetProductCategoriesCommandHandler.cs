using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductCategories;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Products.SetProductCategories;

public sealed class SetProductCategoriesCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetProductCategoriesCommand, Guid>
{
    public async ValueTask<Guid> Handle(SetProductCategoriesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool productExists = await db.Products
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (!productExists)
            throw new NotFoundException($"Product {command.ProductId} not found.");

        // Normalize the desired set: dedupe by CategoryId, ensure exactly one primary.
        var desired = command.Categories
            .GroupBy(c => c.CategoryId)
            .Select(g => new ProductCategoryAssignment(g.Key, g.Any(x => x.IsPrimary)))
            .ToList();

        if (desired.Count > 0 && !desired.Exists(d => d.IsPrimary))
            desired[0] = desired[0] with { IsPrimary = true };

        // Validate every category exists and is not deleted.
        if (desired.Count > 0)
        {
            var desiredIds = desired.Select(d => d.CategoryId).ToList();
            var foundCount = await db.Categories
                .AsNoTracking()
                .CountAsync(c => !c.IsDeleted && desiredIds.Contains(c.Id), cancellationToken)
                .ConfigureAwait(false);

            if (foundCount != desiredIds.Count)
                throw new CustomException(
                    "Una o más categorías no existen o están eliminadas.",
                    Enumerable.Empty<string>(),
                    HttpStatusCode.BadRequest);
        }

        var existing = await db.Set<ProductCategory>()
            .Where(pc => pc.ProductId == command.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var desiredById = desired.ToDictionary(d => d.CategoryId);

        // Remove associations no longer desired.
        var toRemove = existing.Where(e => !desiredById.ContainsKey(e.CategoryId)).ToList();
        if (toRemove.Count > 0)
            db.Set<ProductCategory>().RemoveRange(toRemove);

        // Add new ones; update IsPrimary on the kept ones (no remove+add of the same
        // pair → no unique-index conflict on (ProductId, CategoryId)).
        foreach (var d in desired)
        {
            var match = existing.Find(e => e.CategoryId == d.CategoryId);
            if (match is null)
                db.Set<ProductCategory>().Add(ProductCategory.Create(command.ProductId, d.CategoryId, d.IsPrimary));
            else
                match.SetPrimary(d.IsPrimary);
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return command.ProductId;
    }
}
