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

    // Invariante "exactamente una principal" movida al agregado Party (PR-D2): ver
    // Party.AddCiiuActivity / Party.SetPrincipalCiiu. SetPrincipal lo llama el agregado.
    internal void SetPrincipal(bool value) => IsPrincipal = value;
}
