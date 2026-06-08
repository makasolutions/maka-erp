using FluentValidation;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;

namespace FSH.Modules.Parties.Features.v1.Parties.UpdateParty;

public sealed class UpdatePartyCommandValidator : AbstractValidator<UpdatePartyCommand>
{
    public UpdatePartyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.LegalName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.CreditCurrency).Length(3).When(x => !string.IsNullOrWhiteSpace(x.CreditCurrency));
        RuleFor(x => x.CreditLimit).InclusiveBetween(0, 100_000_000).When(x => x.CreditLimit.HasValue)
            .WithMessage("El límite de crédito no puede superar $100.000.000.");
        RuleForEach(x => x.Contacts).ChildRules(c => c.RuleFor(i => i.Reference).NotEmpty().MaximumLength(128));
        RuleForEach(x => x.Channels).ChildRules(c =>
        {
            c.RuleFor(i => i.ChannelTypeCode).NotEmpty().MaximumLength(64);
            c.RuleFor(i => i.Value).NotEmpty().MaximumLength(256);
        });
    }
}
