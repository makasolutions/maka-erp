using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.RestoreParty;

public sealed record RestorePartyCommand(Guid Id) : ICommand<Guid>;
