using FSH.Modules.SharedRecords.Data;
using FSH.Modules.SharedRecords.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.SharedRecords.Features;

/// <summary>
/// Persiste la invariante "un solo teléfono principal por owner" sin violar el índice único PARCIAL
/// (no diferible al ser índice parcial). Mismo patrón demote→promote que
/// <see cref="AddressPrimaryWriter"/>: degrada TODOS los principales del owner (estado intermedio =
/// 0 principales, válido para el índice), hace flush, y recién entonces promueve el objetivo.
/// </summary>
internal static class PhonePrimaryWriter
{
    public static async Task MakePrimaryAsync(SharedRecordsDbContext db, Phone target, CancellationToken cancellationToken)
    {
        await using var trx = await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // Fase 1 — degradar todos los principales del owner (incl. el objetivo) → 0 principales.
        var currentPrimaries = await db.Phones
            .Where(p => p.OwnerType == target.OwnerType && p.OwnerId == target.OwnerId && p.IsPrimary)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var phone in currentPrimaries)
        {
            phone.SetPrimary(false);
        }
        target.SetPrimary(false); // por si el objetivo es una fila nueva (Added) aún no consultada
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Fase 2 — promover el objetivo → exactamente 1 principal.
        target.SetPrimary(true);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await trx.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
