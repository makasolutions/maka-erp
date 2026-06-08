using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Domain;

namespace FSH.Modules.Parties.Features;

/// <summary>Mapea inputs de hijos (Contracts) a entidades de dominio. Reusado por Create/Update.</summary>
internal static class PartyMapping
{
    /// <summary>Empleado es exclusivo: si está presente, ignora Customer/Supplier.</summary>
    public static PartyRole NormalizeRoles(PartyRole roles) =>
        roles.HasFlag(PartyRole.Employee) ? PartyRole.Employee : roles;

    /// <summary>Persona natural ⇒ "Nombres Apellidos"; jurídica ⇒ razón social.</summary>
    public static string ResolveLegalName(PartyKind kind, string legalName, string? firstName, string? lastName)
    {
        if (kind == PartyKind.Natural)
        {
            string full = $"{firstName?.Trim()} {lastName?.Trim()}".Trim();
            if (!string.IsNullOrWhiteSpace(full)) return full;
        }
        return legalName.Trim();
    }

    public static IEnumerable<PartyAddress> ToAddresses(IReadOnlyList<PartyAddressInput>? items) =>
        (items ?? []).Select(a => PartyAddress.Create(a.Country, a.Department, a.City, a.Line,
            a.Barrio, a.Reference, a.Latitude, a.Longitude, a.IsPrimary, a.LabelCode));

    public static IEnumerable<PartyContact> ToContacts(IReadOnlyList<PartyContactInput>? items) =>
        (items ?? []).Select(c => PartyContact.Create(c.Reference, c.ContactTypeCode, c.AreaCode,
            c.IdentificationTypeCode, c.IdentificationNumber, c.FirstName, c.LastName, c.PositionCode,
            c.ProfessionCode, c.BirthDate, c.GenderCode, c.MaritalStatusCode, c.Email, c.Phone, c.Cell,
            c.IsCommercial, c.Notes));

    public static IEnumerable<PartyChannel> ToChannels(IReadOnlyList<PartyChannelInput>? items) =>
        (items ?? []).Select(c => PartyChannel.Create(c.ChannelTypeCode, c.Value, c.Reference, c.IsPrimary));

    public static IEnumerable<PartyTeamMember> ToTeam(IReadOnlyList<PartyTeamMemberInput>? items) =>
        (items ?? []).Select(m => PartyTeamMember.Create(m.UserId, m.Role));
}
