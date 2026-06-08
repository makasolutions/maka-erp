using FluentValidation;
using FSH.Modules.Parties.Contracts.v1.Parties.CreateParty;
using FSH.Modules.Parties.Domain;

namespace FSH.Modules.Parties.Features.v1.Parties.CreateParty;

public sealed class CreatePartyCommandValidator : AbstractValidator<CreatePartyCommand>
{
    public CreatePartyCommandValidator()
    {
        RuleFor(x => x.IdentificationTypeCode).NotEmpty().WithMessage("El tipo de identificación es obligatorio.").MaximumLength(64);
        RuleFor(x => x.IdentificationNumber).NotEmpty().WithMessage("El número de identificación es obligatorio.").MaximumLength(64);
        RuleFor(x => x)
            .Must(x => IdentificationValidator.Validate(x.IdentificationTypeCode, x.IdentificationNumber, x.VerificationDigit) is null)
            .WithMessage(x => IdentificationValidator.Validate(x.IdentificationTypeCode, x.IdentificationNumber, x.VerificationDigit))
            .WithName(nameof(CreatePartyCommand.IdentificationNumber));
        RuleFor(x => x.LegalName).NotEmpty().WithMessage("La razón social o nombre es obligatorio.").MaximumLength(200);
        RuleFor(x => x.Email).EmailAddress().WithMessage("El correo electrónico no es válido.").MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.CreditCurrency).Length(3).WithMessage("La moneda debe tener 3 caracteres (código ISO).")
            .When(x => !string.IsNullOrWhiteSpace(x.CreditCurrency));
        RuleFor(x => x.CreditLimit).InclusiveBetween(0, 100_000_000).When(x => x.CreditLimit.HasValue)
            .WithMessage("El límite de crédito no puede superar $100.000.000.");
        RuleForEach(x => x.Contacts).ChildRules(c =>
        {
            c.RuleFor(i => i.Email).NotEmpty().WithMessage("Cada persona de contacto requiere un correo.")
                .EmailAddress().WithMessage("El correo de una persona de contacto no es válido.")
                .MaximumLength(256);
            c.RuleFor(i => i.Cell).NotEmpty().WithMessage("Cada persona de contacto requiere un celular.")
                .MaximumLength(64);
        });
        RuleForEach(x => x.Channels).ChildRules(c =>
        {
            c.RuleFor(i => i.ChannelTypeCode).NotEmpty().WithMessage("Cada canal de contacto requiere un tipo.").MaximumLength(64);
            c.RuleFor(i => i.Value).NotEmpty().WithMessage("Cada canal de contacto requiere un valor.").MaximumLength(256);
        });
    }
}
