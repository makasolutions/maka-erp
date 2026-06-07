using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.SetCategoryRequirements;

/// <summary>
/// Replaces the required-attribute set for one (category, marketplace) pair.
/// An empty AttributeIds clears the requirements for that marketplace.
/// </summary>
public sealed class SetCategoryRequirementsCommandHandler(CatalogDbContext db)
    : ICommandHandler<SetCategoryRequirementsCommand, int>
{
    public async ValueTask<int> Handle(SetCategoryRequirementsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var existing = await db.MarketplaceAttributeRequirements
            .Where(r => r.CategoryId == command.CategoryId && r.Marketplace == command.Marketplace)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        db.MarketplaceAttributeRequirements.RemoveRange(existing);

        foreach (var attributeId in command.AttributeIds.Distinct())
            db.MarketplaceAttributeRequirements.Add(
                MarketplaceAttributeRequirement.Create(command.Marketplace, command.CategoryId, attributeId));

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return command.AttributeIds.Distinct().Count();
    }
}
