using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.Profiles;

/// <summary>
/// Faceta socio/accionista (SPEC §6.4). PR-A: entidad independiente con <c>PartyId</c> Guid.
/// </summary>
public sealed class PartnerProfile : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public decimal SharePercentage { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public string? Status { get; private set; }

    private PartnerProfile() { }

    public static PartnerProfile Create(
        Guid partyId,
        decimal sharePercentage,
        DateOnly startDate,
        DateOnly? endDate = null,
        string? status = null)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        if (sharePercentage is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(sharePercentage), "El porcentaje de participación debe estar entre 0 y 100.");
        if (endDate is { } fin && fin < startDate) throw new ArgumentOutOfRangeException(nameof(endDate), "La fecha fin no puede ser anterior al inicio.");
        return new PartnerProfile
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            SharePercentage = sharePercentage,
            StartDate = startDate,
            EndDate = endDate,
            Status = status?.Trim(),
        };
    }
}
