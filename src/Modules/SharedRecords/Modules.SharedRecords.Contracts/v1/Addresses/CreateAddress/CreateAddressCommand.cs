using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Addresses.CreateAddress;

public sealed record CreateAddressCommand(
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
    decimal? Longitude) : ICommand<Guid>;
