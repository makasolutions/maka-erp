using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain.Profiles;

/// <summary>
/// Faceta contacto B2B (SPEC §6.3). La relación con la empresa madre es <c>Party.ParentPartyId</c>
/// (regla R4), NO un campo de este perfil.
///
/// PR-A: entidad independiente con <c>PartyId</c> Guid (sin nav en Party).
/// </summary>
public sealed class ContactProfile : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public string? JobTitle { get; private set; }
    public ContactFunction? ContactFunction { get; private set; }
    public bool IsCommercialContact { get; private set; }
    public bool IsPrimary { get; private set; }
    public Guid? ResponsibleUserId { get; private set; }

    private ContactProfile() { }

    public static ContactProfile Create(
        Guid partyId,
        string? jobTitle = null,
        ContactFunction? contactFunction = null,
        bool isCommercialContact = false,
        bool isPrimary = false,
        Guid? responsibleUserId = null)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        return new ContactProfile
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            JobTitle = jobTitle?.Trim(),
            ContactFunction = contactFunction,
            IsCommercialContact = isCommercialContact,
            IsPrimary = isPrimary,
            ResponsibleUserId = responsibleUserId,
        };
    }
}
