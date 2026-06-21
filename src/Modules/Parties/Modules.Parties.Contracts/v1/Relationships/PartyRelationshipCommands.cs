using System.Text.Json;
using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Relationships;

/// <summary>Crea un vínculo persona↔empresa (modo live, edición). La persona es DISCRIMINADA:
/// <c>SourcePartyId</c> (existente, reusa) O <c>NewPerson</c> (nueva inline, creada en la MISMA
/// transacción → sin huérfanos). Si <c>IsPrimary</c>, el handler desmarca el principal anterior de esa
/// empresa (invariante "uno por empresa"). <c>CustomFields</c> = valores scope PartyRelationship.
/// Permiso <c>Parties.Relationships.Manage</c>.</summary>
public sealed record CreatePartyRelationshipCommand(
    Guid?    SourcePartyId,
    NewPersonInput? NewPerson,
    Guid     TargetPartyId,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    bool     IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate,
    IReadOnlyDictionary<string, JsonElement>? CustomFields = null) : ICommand<Guid>;

/// <summary>Edita rol/función/cargo/vigencia + custom fields de un vínculo. No cambia Source/Target.</summary>
public sealed record UpdatePartyRelationshipCommand(
    Guid     Id,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    DateOnly StartDate,
    DateOnly? EndDate,
    IReadOnlyDictionary<string, JsonElement>? CustomFields = null) : ICommand;

/// <summary>Marca/desmarca el vínculo como principal de su empresa (desmarca el anterior).</summary>
public sealed record SetPrimaryPartyRelationshipCommand(Guid Id, bool IsPrimary) : ICommand;

/// <summary>Baja lógica del vínculo (no borra la persona ni sus otras relaciones).</summary>
public sealed record DeletePartyRelationshipCommand(Guid Id) : ICommand;

/// <summary>Lista los vínculos cuya empresa (<c>TargetPartyId</c>) es la dada. Por defecto solo activos.</summary>
public sealed record GetPartyRelationshipsQuery(Guid TargetPartyId, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<PartyRelationshipDto>>;

/// <summary>Dirección B (PR-3, solo lectura): lista las empresas a las que está vinculada una persona
/// (<c>SourcePartyId</c>) con su cargo/función. Por defecto solo activos.</summary>
public sealed record GetPartyRelationshipsBySourceQuery(Guid SourcePartyId, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<PartyRelationshipBySourceDto>>;
