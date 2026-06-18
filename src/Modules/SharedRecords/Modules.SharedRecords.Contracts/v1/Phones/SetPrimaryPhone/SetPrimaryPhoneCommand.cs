using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Phones.SetPrimaryPhone;

/// <summary>Marca este teléfono como principal del owner; degrada los demás (idempotente).</summary>
public sealed record SetPrimaryPhoneCommand(Guid Id) : ICommand;
