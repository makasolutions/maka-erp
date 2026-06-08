using FSH.Modules.Parties.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Parties.Contracts.v1.Parties.SetPartyRoles;

public sealed record SetPartyRolesCommand(Guid Id, PartyRole Roles) : ICommand<Guid>;
