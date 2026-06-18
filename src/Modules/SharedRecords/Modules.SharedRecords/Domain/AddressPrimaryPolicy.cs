namespace FSH.Modules.SharedRecords.Domain;

/// <summary>
/// Regla de dominio idempotente "una sola principal por owner" (patrón de las reglas idempotentes
/// de la migración Parties). Dada la colección de direcciones de UN owner, deja exactamente la
/// indicada como principal y degrada las demás. Re-ejecutarla con el mismo input no cambia nada.
/// </summary>
public static class AddressPrimaryPolicy
{
    /// <summary>Promueve <paramref name="primaryId"/> y degrada el resto. Si no está en la lista,
    /// todas quedan no-principales (caso borrado de la principal).</summary>
    public static void EnsureSinglePrimary(IEnumerable<Address> ownerAddresses, Guid primaryId)
    {
        ArgumentNullException.ThrowIfNull(ownerAddresses);
        foreach (var address in ownerAddresses)
        {
            address.SetPrimary(address.Id == primaryId);
        }
    }
}
