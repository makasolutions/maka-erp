using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Relationships;

/// <summary>Crea un vínculo persona↔empresa. Si <c>IsPrimary</c>, el handler desmarca el principal
/// anterior de esa empresa (invariante "uno por empresa"). Permiso <c>Parties.Relationships.Manage</c>.</summary>
public sealed record CreatePartyRelationshipCommand(
    Guid     SourcePartyId,
    Guid     TargetPartyId,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    bool     IsPrimary,
    DateOnly? StartDate,
    DateOnly? EndDate) : ICommand<Guid>;

/// <summary>Edita rol/función/cargo/vigencia de un vínculo. No cambia Source/Target.</summary>
public sealed record UpdatePartyRelationshipCommand(
    Guid     Id,
    string   RelationshipTypeCode,
    string?  ContactFunctionCode,
    string?  JobTitleCode,
    DateOnly StartDate,
    DateOnly? EndDate) : ICommand;

/// <summary>Marca/desmarca el vínculo como principal de su empresa (desmarca el anterior).</summary>
public sealed record SetPrimaryPartyRelationshipCommand(Guid Id, bool IsPrimary) : ICommand;

/// <summary>Baja lógica del vínculo (no borra la persona ni sus otras relaciones).</summary>
public sealed record DeletePartyRelationshipCommand(Guid Id) : ICommand;

/// <summary>Lista los vínculos cuya empresa (<c>TargetPartyId</c>) es la dada. Por defecto solo activos.</summary>
public sealed record GetPartyRelationshipsQuery(Guid TargetPartyId, bool IncludeInactive = false)
    : IQuery<IReadOnlyList<PartyRelationshipDto>>;
