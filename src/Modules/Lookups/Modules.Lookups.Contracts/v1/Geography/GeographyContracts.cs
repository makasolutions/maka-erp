using Mediator;

namespace FSH.Modules.Lookups.Contracts.v1.Geography;

/// <summary>Municipio DIVIPOLA (código 5 dígitos + nombre).</summary>
public sealed record MunicipalityDto(string Code, string Name);

/// <summary>Departamento DIVIPOLA con sus municipios (para poblar el CityPicker de cascada).</summary>
public sealed record DepartmentDto(string Code, string Name, IReadOnlyList<MunicipalityDto> Municipalities);

/// <summary>Todos los departamentos con sus municipios. Dato global, cacheable.</summary>
public sealed record GetDepartmentsQuery : IQuery<IReadOnlyList<DepartmentDto>>;

/// <summary>Municipios de un departamento por su código DIVIPOLA (2 dígitos).</summary>
public sealed record GetMunicipalitiesQuery(string DepartmentCode) : IQuery<IReadOnlyList<MunicipalityDto>>;
