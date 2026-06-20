using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Contracts.v1.Parties;

public sealed record PartyAddressInput(
    string  Country,
    string? Department,
    string? City,
    string? Line,
    string? Barrio,
    string? Reference,
    decimal? Latitude,
    decimal? Longitude,
    bool    IsPrimary,
    string? LabelCode,
    string? DepartmentCode = null,
    string? MunicipalityCode = null);

// PR-2: PartyContactInput ELIMINADO — los contactos persona↔empresa se capturan vía PartyRelationship
// (ContactList, PR-3), no como hijos del comando de Party.

public sealed record PartyChannelInput(
    string  ChannelTypeCode,
    string  Value,
    string? Reference,
    bool    IsPrimary);

public sealed record PartyTeamMemberInput(
    Guid          UserId,
    PartyTeamRole Role);
