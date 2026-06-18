namespace FSH.Modules.SharedRecords.Domain;

/// <summary>
/// Regla de dominio idempotente "un solo teléfono principal por owner" (mismo patrón que
/// <see cref="AddressPrimaryPolicy"/>). Dada la colección de teléfonos de UN owner, deja
/// exactamente el indicado como principal y degrada los demás. Re-ejecutarla no cambia nada.
/// </summary>
public static class PhonePrimaryPolicy
{
    /// <summary>Promueve <paramref name="primaryId"/> y degrada el resto. Si no está en la lista,
    /// todos quedan no-principales (caso borrado del principal).</summary>
    public static void EnsureSinglePrimary(IEnumerable<Phone> ownerPhones, Guid primaryId)
    {
        ArgumentNullException.ThrowIfNull(ownerPhones);
        foreach (var phone in ownerPhones)
        {
            phone.SetPrimary(phone.Id == primaryId);
        }
    }
}
