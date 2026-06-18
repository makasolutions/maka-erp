using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Addresses.UpdateAddress;

/// <summary>Actualiza una dirección. El owner es inmutable (no se reasigna por update).</summary>
public sealed record UpdateAddressCommand(
    Guid    Id,
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
    decimal? Longitude) : ICommand<Guid>;
