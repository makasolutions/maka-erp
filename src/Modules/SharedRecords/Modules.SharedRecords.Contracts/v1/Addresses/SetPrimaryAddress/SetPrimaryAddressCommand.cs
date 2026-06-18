using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Addresses.SetPrimaryAddress;

/// <summary>Marca esta dirección como principal del owner; degrada las demás (idempotente).</summary>
public sealed record SetPrimaryAddressCommand(Guid Id) : ICommand;
