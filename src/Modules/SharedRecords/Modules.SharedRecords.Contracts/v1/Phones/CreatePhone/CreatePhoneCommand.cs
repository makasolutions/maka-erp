using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Phones.CreatePhone;

public sealed record CreatePhoneCommand(
    string  OwnerType,
    Guid    OwnerId,
    string? TypeCode,
    bool    IsActive,
    bool    IsPrimary,
    string  Number,
    string? Extension,
    string? CountryCode) : ICommand<Guid>;
