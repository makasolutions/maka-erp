using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.ApprovePriceProposals;
using FSH.Modules.Catalog.Data;
using FSH.Modules.Catalog.Domain;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.ApprovePriceProposals;

public sealed class ApprovePriceProposalsCommandHandler(CatalogDbContext db, ICurrentUser currentUser)
    : ICommandHandler<ApprovePriceProposalsCommand, int>
{
    public async ValueTask<int> Handle(ApprovePriceProposalsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var proposals = await db.PriceBulkProposals
            .Where(p => p.BatchId == command.BatchId && p.Status == PriceProposalStatus.Pending)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (proposals.Count == 0)
            throw new NotFoundException($"No pending proposals found for batch {command.BatchId}.");

        string userId = currentUser.GetUserId().ToString();

        // Load existing items referenced by the proposals (tracked, for ChangePrice).
        var itemIds = proposals.Where(p => p.PriceListItemId.HasValue)
                               .Select(p => p.PriceListItemId!.Value)
                               .ToList();
        var items = itemIds.Count == 0
            ? new Dictionary<Guid, PriceListItem>()
            : await db.PriceListItems
                .Where(i => itemIds.Contains(i.Id))
                .ToDictionaryAsync(i => i.Id, cancellationToken)
                .ConfigureAwait(false);

        int applied = 0;
        foreach (var proposal in proposals)
        {
            if (proposal.PriceListItemId.HasValue && items.TryGetValue(proposal.PriceListItemId.Value, out var item))
            {
                // ChangePrice records the immutable history entry (spec §7).
                item.ChangePrice(
                    proposal.NewPrice,
                    userId,
                    proposal.ChangeReason ?? "Importación masiva de precios",
                    proposal.SourceReference);
            }
            else
            {
                var newItem = PriceListItem.Create(
                    proposal.PriceListId,
                    proposal.VariationId,
                    proposal.NewPrice,
                    userId);
                db.PriceListItems.Add(newItem);
            }

            proposal.Approve(userId);
            applied++;
        }

        // Single SaveChanges wraps everything in one transaction (atomic).
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return applied;
    }
}
