using System.Text.Json;

namespace FSH.Modules.Parties.Contracts.v1.Relationships;

/// <summary>Vínculo M2M persona↔empresa visto desde la EMPRESA (dirección A, PR-3). <c>SourceName</c>,
/// <c>SourcePrimaryChannel</c> e <c>SourceIsPEP</c> se resuelven en el backend (de la persona-Source) para
/// el listado sin N+1. <c>CustomFieldsJson</c> = valores de custom fields (scope PartyRelationship) tal
/// como están en el JSONB, para precargar el editor. Los códigos son de Tabla Básica.</summary>
public sealed record PartyRelationshipDto(
    Guid     Id,
    Guid     SourcePartyId,
    string?  SourceName,
    string?  SourcePrimaryChannel,
    bool     SourceIsPEP,
    Guid     TargetPartyId,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    bool     IsPrimary,
    bool     IsActive,
    DateOnly StartDate,
    DateOnly? EndDate,
    string?  CustomFieldsJson);

/// <summary>Vínculo visto desde la PERSONA (dirección B, PR-3, solo lectura): a qué empresas está
/// vinculada y con qué cargo/función. <c>TargetName</c> = nombre de la empresa, resuelto en backend.</summary>
public sealed record PartyRelationshipBySourceDto(
    Guid     Id,
    Guid     TargetPartyId,
    string?  TargetName,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    bool     IsPrimary,
    bool     IsActive,
    DateOnly StartDate,
    DateOnly? EndDate);

/// <summary>Persona nueva mínima "stageada" en el buscar-o-crear (PR-3, Opción B). El dominio exige
/// identificación (no solo nombre): es el mínimo válido de un <c>Party</c>. La persona-Party se crea
/// server-side en la MISMA transacción que la relación (cero huérfanos). Kind siempre Natural.</summary>
public sealed record NewPersonInput(
    string  IdentificationTypeCode,
    string  IdentificationNumber,
    int?    VerificationDigit,
    string? FirstName,
    string? LastName,
    string? LegalName);

/// <summary>Una línea de relación para la creación atómica del tercero (Opción B): referencia
/// DISCRIMINADA a persona existente (<c>SourcePartyId</c>) O persona nueva inline (<c>NewPerson</c>).
/// El handler resuelve-o-crea la persona y crea la relación en la misma transacción que la empresa.</summary>
public sealed record PartyRelationshipLineInput(
    Guid?    SourcePartyId,
    NewPersonInput? NewPerson,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    bool     IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyDictionary<string, JsonElement>? CustomFields);
