using FluentValidation;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties.UpdateParty;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Features.v1.Parties;

namespace FSH.Modules.Parties.Features.v1.Parties.UpdateParty;

public sealed class UpdatePartyCommandValidator : AbstractValidator<UpdatePartyCommand>
{
    public UpdatePartyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.LegalName).NotEmpty().WithMessage("La razón social o nombre es obligatorio.")
            .MaximumLength(FormValidationRules.LegalNameMaxLength).WithMessage("La razón social no puede superar 150 caracteres.");

        RuleFor(x => x.FirstName).NotEmpty().WithMessage("Los nombres son obligatorios para persona natural.")
            .When(x => x.Kind == PartyKind.Natural);
        RuleFor(x => x.FirstName).MaximumLength(FormValidationRules.NameMaxLength).WithMessage("Los nombres no pueden superar 50 caracteres.")
            .Must(FormValidationRules.IsValidPersonName).WithMessage("Los nombres contienen caracteres no válidos.")
            .When(x => !string.IsNullOrWhiteSpace(x.FirstName));
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Los apellidos son obligatorios para persona natural.")
            .When(x => x.Kind == PartyKind.Natural);
        RuleFor(x => x.LastName).MaximumLength(FormValidationRules.NameMaxLength).WithMessage("Los apellidos no pueden superar 50 caracteres.")
            .Must(FormValidationRules.IsValidPersonName).WithMessage("Los apellidos contienen caracteres no válidos.")
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));

        RuleFor(x => x.Email).EmailAddress().WithMessage("El correo electrónico no es válido.").MaximumLength(256)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Website).Must(FormValidationRules.IsValidUrl).WithMessage("La página web no es una URL válida.")
            .MaximumLength(FormValidationRules.UrlMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Website));

        RuleFor(x => x).Must(x => FormValidationRules.BirthDateError(x.BirthDate) is null)
            .WithMessage(x => FormValidationRules.BirthDateError(x.BirthDate))
            .WithName(nameof(UpdatePartyCommand.BirthDate))
            .When(x => x.BirthDate.HasValue);

        RuleFor(x => x.CreditCurrency).Length(3).WithMessage("La moneda debe tener 3 caracteres (código ISO).")
            .When(x => !string.IsNullOrWhiteSpace(x.CreditCurrency));
        RuleFor(x => x.CreditLimit).InclusiveBetween(0, 100_000_000).When(x => x.CreditLimit.HasValue)
            .WithMessage("El límite de crédito no puede superar $100.000.000.");

        RuleForEach(x => x.Addresses).SetValidator(new PartyAddressInputValidator());
        RuleForEach(x => x.Channels).SetValidator(new PartyChannelInputValidator());
    }
}
