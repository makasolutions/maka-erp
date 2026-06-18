using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Phones.UpdatePhone;

/// <summary>Actualiza un teléfono. El owner es inmutable (no se reasigna por update).</summary>
public sealed record UpdatePhoneCommand(
    Guid    Id,
    string? TypeCode,
    bool    IsActive,
    bool    IsPrimary,
    string  Number,
    string? Extension,
    string? CountryCode) : ICommand<Guid>;
