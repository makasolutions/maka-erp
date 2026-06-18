using FluentValidation;
using FSH.Modules.SharedRecords.Contracts.v1.Phones.UpdatePhone;
using FSH.Modules.SharedRecords.Features.v1.Phones.CreatePhone;

namespace FSH.Modules.SharedRecords.Features.v1.Phones.UpdatePhone;

public sealed class UpdatePhoneCommandValidator : AbstractValidator<UpdatePhoneCommand>
{
    public UpdatePhoneCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(32)
            .Matches(CreatePhoneCommandValidator.NumberPattern).WithMessage("El número de teléfono no es válido.");
        RuleFor(x => x.TypeCode).MaximumLength(64);
        RuleFor(x => x.Extension).MaximumLength(16).Matches(@"^\d{1,8}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Extension)).WithMessage("La extensión debe ser numérica.");
        RuleFor(x => x.CountryCode).MaximumLength(8).Matches(@"^\+?\d{1,4}$")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryCode)).WithMessage("El indicativo no es válido.");
    }
}
