using FluentValidation;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.CreatePhone;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.CreatePhone;

public sealed class CreatePhoneCommandValidator : AbstractValidator<CreatePhoneCommand>
{
    // Acepta nacional ("3201234567"), con separadores ("(601) 234-5678") e internacional ("+57 320…").
    internal const string NumberPattern = @"^\+?[0-9\s\-()]{7,32}$";

    public CreatePhoneCommandValidator()
    {
        RuleFor(x => x.OwnerType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(32)
            .Matches(NumberPattern).WithMessage("El número de teléfono no es válido.");
        RuleFor(x => x.TypeCode).MaximumLength(64);
        RuleFor(x => x.Extension).MaximumLength(16).Matches(@"^\d{1,8}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Extension)).WithMessage("La extensión debe ser numérica.");
        RuleFor(x => x.CountryCode).MaximumLength(8).Matches(@"^\+?\d{1,4}$")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode)).WithMessage("El indicativo no es válido.");
    }
}
