using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.Profiles;

/// <summary>
/// Faceta contacto del Party — **adelgazada en PR-2**. Solo conserva atributos GLOBALES de la persona
/// (p. ej. <see cref="ResponsibleUserId"/>). Lo *por-empresa* (cargo, función, principal) migró a
/// <c>PartyRelationship</c> (M2M). La relación con la empresa madre sigue siendo <c>Party.ParentPartyId</c>
/// (jerarquía organizacional), NO esta faceta.
/// </summary>
public sealed class ContactProfile : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public Guid? ResponsibleUserId { get; private set; }

    private ContactProfile() { }

    public static ContactProfile Create(Guid partyId, Guid? responsibleUserId = null)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        return new ContactProfile
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            ResponsibleUserId = responsibleUserId,
        };
    }
}
