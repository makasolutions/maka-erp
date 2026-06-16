using System.Net;
using FSH.Framework.Core.Exceptions;
using FSH.Modules.Parties.Contracts.v1.Parties.SetPartyRoles;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Features;
using FSH.Modules.Parties.Features.Sync;
using Mediator;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Parties.Features.v1.Parties.SetPartyRoles;

public sealed class SetPartyRolesCommandHandler(PartiesDbContext db, PartyV2Synchronizer synchronizer)
    : ICommandHandler<SetPartyRolesCommand, Guid>
{
    public async ValueTask<Guid> Handle(SetPartyRolesCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Navs de profile cargadas para que el synchronizer diffee las facetas (crear/reactivar/desactivar).
        var party = await db.Parties
            .Include(p => p.CustomerProfile).Include(p => p.SupplierProfile).Include(p => p.EmployeeProfile)
            .FirstOrDefaultAsync(p => p.Id == command.Id && !p.IsDeleted, cancellationToken).ConfigureAwait(false)
            ?? throw new CustomException("Tercero no encontrado.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        var roles = PartyMapping.NormalizeRoles(command.Roles);
        party.SetRoles(roles); // sigue escribiendo la columna v1 (hasta F1b); fuente de verdad = profiles

        // PR-F1a: SetPartyRoles ahora SÍ sincroniza profiles (antes solo escribía el flag v1 — gap
        // pre-D5). Sin esto, el output computado desde v2 divergiría de la acción de roles.
        await synchronizer.SyncFacetsAsync(party, roles, cancellationToken).ConfigureAwait(false);

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return party.Id;
    }
}
