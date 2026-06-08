using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.GetPartyById;

public sealed record GetPartyByIdQuery(Guid Id) : IQuery<PartyDetailDto>;
