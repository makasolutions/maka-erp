using FluentValidation;
using FSH.Modules.Parties.Contracts.v1.Parties.SetPartyRoles;

namespace FSH.Modules.Parties.Features.v1.Parties.SetPartyRoles;

public sealed class SetPartyRolesCommandValidator : AbstractValidator<SetPartyRolesCommand>
{
    public SetPartyRolesCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
