using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.v1.ProductTags.SetProductTags;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.ProductTags.SetProductTags;

public sealed class SetProductTagsCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetProductTagsCommand, Guid>
{
    public async ValueTask<Guid> Handle(SetProductTagsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        bool productExists = await db.Products
            .AsNoTracking()
            .AnyAsync(p => !p.IsDeleted && p.Id == command.ProductId, cancellationToken)
            .ConfigureAwait(false);

        if (!productExists)
            throw new NotFoundException($"Product {command.ProductId} not found.");

        // Dedupe by case-insensitive name; replace the full set.
        var desired = command.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t.Name))
            .GroupBy(t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var existing = await db.Set<ProductTag>()
            .Where(t => t.ProductId == command.ProductId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing.Count > 0)
            db.Set<ProductTag>().RemoveRange(existing);

        foreach (var tag in desired)
            db.Set<ProductTag>().Add(ProductTag.Create(command.ProductId, tag.Name, tag.Color));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return command.ProductId;
    }
}
