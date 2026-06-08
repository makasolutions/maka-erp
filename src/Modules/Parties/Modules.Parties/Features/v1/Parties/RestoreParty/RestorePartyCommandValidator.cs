using FluentValidation;
using FSH.Modules.Parties.Contracts.v1.Parties.RestoreParty;

namespace FSH.Modules.Parties.Features.v1.Parties.RestoreParty;

public sealed class RestorePartyCommandValidator : AbstractValidator<RestorePartyCommand>
{
    public RestorePartyCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
