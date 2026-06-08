using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.SetPartyRoles;
using FSH.Modules.Parties.Data;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.SetPartyRoles;

public sealed class SetPartyRolesCommandHandler(PartiesDbContext db)
    : ICommandHandler<SetPartyRolesCommand, Guid>
{
    public async ValueTask<Guid> Handle(SetPartyRolesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var party = await db.Parties.FirstOrDefaultAsync(p => p.Id == command.Id && !p.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);
        party.SetRoles(command.Roles);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return party.Id;
    }
}
