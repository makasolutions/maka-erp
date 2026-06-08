using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.DeleteParty;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.DeleteParty;

public sealed class DeletePartyCommandHandler(PartiesDbContext db)
    : ICommandHandler<DeletePartyCommand>
{
    public async ValueTask<Unit> Handle(DeletePartyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var party = await db.Parties.FirstOrDefaultAsync(p => p.Id == command.Id && !p.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        db.Parties.Remove(party); // soft-delete via auditing interceptor
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Unit.Value;
    }
}
