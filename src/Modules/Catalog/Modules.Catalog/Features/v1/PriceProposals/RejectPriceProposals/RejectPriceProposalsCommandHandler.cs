using FSH.Framework.Core.Context;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.RejectPriceProposals;
using FSH.Modules.Catalog.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.RejectPriceProposals;

public sealed class RejectPriceProposalsCommandHandler(CatalogDbContext db, ICurrentUser currentUser)
    : ICommandHandler<RejectPriceProposalsCommand, int>
{
    public async ValueTask<int> Handle(RejectPriceProposalsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var proposals = await db.PriceBulkProposals
            .Where(p => p.BatchId == command.BatchId && p.Status == PriceProposalStatus.Pending)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (proposals.Count == 0)
            throw new NotFoundException($"No pending proposals found for batch {command.BatchId}.");

        string userId = currentUser.GetUserId().ToString();

        foreach (var proposal in proposals)
            proposal.Reject(userId);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return proposals.Count;
    }
}
