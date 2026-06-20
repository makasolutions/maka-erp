namespace FSH.Modules.Parties.Contracts.v1.Relationships;

/// <summary>Vínculo M2M persona↔empresa (PR-2). <c>SourceName</c> = nombre de la persona, resuelto en
/// el backend para el listado. Los códigos son de Tabla Básica (RelationshipType/ContactFunction/Position).</summary>
public sealed record PartyRelationshipDto(
    Guid     Id,
    Guid     SourcePartyId,
    string?  SourceName,
    Guid     TargetPartyId,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    bool     IsPrimary,
    bool     IsActive,
    DateOnly StartDate,
    DateOnly? EndDate);
