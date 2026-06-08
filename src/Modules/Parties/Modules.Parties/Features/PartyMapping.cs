using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Domain;

namespace FSH.Modules.Parties.Features;

/// <summary>Mapea inputs de hijos (Contracts) a entidades de dominio. Reusado por Create/Update.</summary>
internal static class PartyMapping
{
    public static IEnumerable<PartyAddress> ToAddresses(IReadOnlyList<PartyAddressInput>? items) =>
        (items ?? []).Select(a => PartyAddress.Create(a.Country, a.Department, a.City, a.Line,
            a.Reference, a.Latitude, a.Longitude, a.IsPrimary, a.Label));

    public static IEnumerable<PartyContact> ToContacts(IReadOnlyList<PartyContactInput>? items) =>
        (items ?? []).Select(c => PartyContact.Create(c.Reference, c.FullName, c.Email, c.Phone, c.Cell, c.IsCommercial, c.Notes));

    public static IEnumerable<PartyChannel> ToChannels(IReadOnlyList<PartyChannelInput>? items) =>
        (items ?? []).Select(c => PartyChannel.Create(c.ChannelTypeCode, c.Value, c.Reference, c.IsPrimary));

    public static IEnumerable<PartyTeamMember> ToTeam(IReadOnlyList<PartyTeamMemberInput>? items) =>
        (items ?? []).Select(m => PartyTeamMember.Create(m.UserId, m.Role));
}
