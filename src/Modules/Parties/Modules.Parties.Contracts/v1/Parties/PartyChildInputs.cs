using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Contracts.v1.Parties;

public sealed record PartyAddressInput(
    string  Country,
    string? Department,
    string? City,
    string? Line,
    string? Reference,
    decimal? Latitude,
    decimal? Longitude,
    bool    IsPrimary,
    string? Label);

public sealed record PartyContactInput(
    string  Reference,
    string? FullName,
    string? Email,
    string? Phone,
    string? Cell,
    bool    IsCommercial,
    string? Notes);

public sealed record PartyChannelInput(
    string  ChannelTypeCode,
    string  Value,
    string? Reference,
    bool    IsPrimary);

public sealed record PartyTeamMemberInput(
    Guid          UserId,
    PartyTeamRole Role);
