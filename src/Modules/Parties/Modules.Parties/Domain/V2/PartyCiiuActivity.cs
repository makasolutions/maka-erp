using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.V2;

/// <summary>
/// Actividad económica CIIU de un tercero (SPEC §7). El RUT permite múltiples CIIU (1..N),
/// con exactamente una principal — por eso es tabla hija y no un string único como en v1.
///
/// PR-A: entidad independiente con <c>PartyId</c> Guid (sin nav property en Party).
/// </summary>
public sealed class PartyCiiuActivity : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public string CiiuCode { get; private set; } = default!;
    public bool IsPrincipal { get; private set; }

    private PartyCiiuActivity() { }

    public static PartyCiiuActivity Create(Guid partyId, string ciiuCode, bool isPrincipal = false)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        ArgumentException.ThrowIfNullOrWhiteSpace(ciiuCode);
        return new PartyCiiuActivity
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            CiiuCode = ciiuCode.Trim(),
            IsPrincipal = isPrincipal,
        };
    }

    internal void SetPrincipal(bool value) => IsPrincipal = value;
}

/// <summary>
/// Operaciones de dominio sobre la colección de actividades CIIU de un tercero.
///
/// PR-A: helper transitorio; la invariante "una sola principal" se mueve al agregado Party en PR-D
/// cuando Party posea la colección. Aquí vive como helper porque Party no posee las CIIU todavía.
/// </summary>
public static class CiiuActivities
{
    /// <summary>
    /// Marca como principal la actividad <paramref name="principalId"/> y desmarca las demás,
    /// garantizando exactamente una principal. Lanza si el id no está en la colección.
    /// </summary>
    public static void SetPrincipal(IList<PartyCiiuActivity> activities, Guid principalId)
    {
        ArgumentNullException.ThrowIfNull(activities);
        var target = activities.FirstOrDefault(a => a.Id == principalId)
            ?? throw new ArgumentException("La actividad indicada no pertenece a la colección.", nameof(principalId));

        foreach (var a in activities) a.SetPrincipal(false);
        target.SetPrincipal(true);
    }

    /// <summary>True si la colección cumple la invariante: vacía, o exactamente una principal.</summary>
    public static bool HasValidPrincipal(IReadOnlyCollection<PartyCiiuActivity> activities)
    {
        ArgumentNullException.ThrowIfNull(activities);
        if (activities.Count == 0) return true;
        return activities.Count(a => a.IsPrincipal) == 1;
    }
}
