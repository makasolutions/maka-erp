using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.Records.GetBasicRecordsByCode;

/// <summary>
/// Consulta liviana que devuelve los registros ACTIVOS de una tabla por su Code
/// (ej. "IdentificationType"). La usan todos los módulos/frontend para poblar dropdowns.
/// </summary>
public sealed record GetBasicRecordsByCodeQuery(string TableCode)
    : IQuery<IReadOnlyList<BasicRecordBriefDto>>;

public sealed record BasicRecordBriefDto(string Code, string Value, int SortOrder);
