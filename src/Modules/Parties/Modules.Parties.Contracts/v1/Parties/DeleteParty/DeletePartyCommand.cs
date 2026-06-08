using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.DeleteParty;

public sealed record DeletePartyCommand(Guid Id) : ICommand;
