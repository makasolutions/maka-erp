using FluentValidation;
using FSH.Modules.Parties.Contracts.v1.Parties.DeleteParty;

namespace FSH.Modules.Parties.Features.v1.Parties.DeleteParty;

public sealed class DeletePartyCommandValidator : AbstractValidator<DeletePartyCommand>
{
    public DeletePartyCommandValidator() => RuleFor(x => x.Id).NotEmpty();
}
