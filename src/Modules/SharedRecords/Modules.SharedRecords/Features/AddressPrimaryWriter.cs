using FSH.Modules.SharedRecords.Data;
using FSH.Modules.SharedRecords.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features;

/// <summary>
/// Persiste la invariante "una sola principal por owner" sin violar el índice único PARCIAL
/// (no diferible al ser índice parcial). Secuencia demote→promote en una transacción: primero
/// degrada TODAS las principales del owner (estado intermedio = 0 principales, válido para el
/// índice), hace flush, y recién entonces promueve la objetivo. Evita el estado transitorio de
/// dos principales que dispara la violación de unicidad.
/// </summary>
internal static class AddressPrimaryWriter
{
    public static async Task MakePrimaryAsync(SharedRecordsDbContext db, Address target, CancellationToken cancellationToken)
    {
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // Fase 1 — degradar todas las principales del owner (incl. la objetivo) → 0 principales.
        // El filtro global por TenantId (BaseDbContext) acota al tenant actual.
        var currentPrimaries = await db.Addresses
            .Where(a => a.OwnerType == target.OwnerType && a.OwnerId == target.OwnerId && a.IsPrimary)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var address in currentPrimaries)
        {
            address.SetPrimary(false);
        }
        target.SetPrimary(false); // por si la objetivo es una fila nueva (Added) aún no consultada
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Fase 2 — promover la objetivo → exactamente 1 principal.
        target.SetPrimary(true);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await trx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
