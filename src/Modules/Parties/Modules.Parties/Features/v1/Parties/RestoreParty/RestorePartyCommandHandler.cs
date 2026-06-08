using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.RestoreParty;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.RestoreParty;

public sealed class RestorePartyCommandHandler(PartiesDbContext db)
    : ICommandHandler<RestorePartyCommand, Guid>
{
    public async ValueTask<Guid> Handle(RestorePartyCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var party = await db.Parties.IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == command.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        party.Restore();
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return party.Id;
    }
}
