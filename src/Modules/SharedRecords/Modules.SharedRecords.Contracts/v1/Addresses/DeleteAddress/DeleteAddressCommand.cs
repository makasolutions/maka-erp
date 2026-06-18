using Mediator;

namespace FSH.Modules.SharedRecords.Contracts.v1.Addresses.DeleteAddress;

public sealed record DeleteAddressCommand(Guid Id) : ICommand;
