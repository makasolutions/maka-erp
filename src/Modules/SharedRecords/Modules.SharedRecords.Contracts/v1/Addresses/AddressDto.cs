namespace FSH.Modules.SharedRecords.Contracts.v1.Addresses;

/// <summary>
/// Dirección genérica polimórfica: se asocia a cualquier entidad vía <see cref="OwnerType"/> +
/// <see cref="OwnerId"/> (sin FK dura cross-módulo). <see cref="LabelCode"/> = clasificación
/// (Tabla Básica <c>AddressLabel</c>: Bodega/Sede/Sucursal/Oficina…). Una sola principal por owner.
/// </summary>
public sealed record AddressDto(
    Guid    Id,
    string  OwnerType,
    Guid    OwnerId,
    string? LabelCode,
    bool    IsActive,
    bool    IsPrimary,
    string  Country,
    string? Department,
    string? City,
    string? DepartmentCode,
    string? MunicipalityCode,
    string? Line,
    string? Barrio,
    string? Reference,
    decimal? Latitude,
    decimal? Longitude);
