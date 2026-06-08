using FluentValidation;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Domain;

namespace FSH.Modules.Parties.Features.v1.Parties;

public sealed class PartyAddressInputValidator : AbstractValidator<PartyAddressInput>
{
    public PartyAddressInputValidator()
    {
        RuleFor(a => a.City).NotEmpty().WithMessage("Cada dirección requiere una ciudad.");
        RuleFor(a => a.Line).NotEmpty().WithMessage("Cada dirección requiere la dirección (calle/carrera).")
            .Must(FormValidationRules.IsValidColombianAddress)
            .WithMessage("La dirección no cumple la nomenclatura. Ej.: CL 100 # 13-21.")
            .When(a => !string.IsNullOrWhiteSpace(a.Line));
        RuleFor(a => a.Latitude).Must(FormValidationRules.IsValidLatitude)
            .WithMessage("La latitud debe estar entre -90 y 90.");
        RuleFor(a => a.Longitude).Must(FormValidationRules.IsValidLongitude)
            .WithMessage("La longitud debe estar entre -180 y 180.");
        RuleFor(a => a).Must(a => a.Latitude.HasValue == a.Longitude.HasValue)
            .WithMessage("Latitud y longitud deben ingresarse juntas.")
            .WithName(nameof(PartyAddressInput.Latitude));
    }
}

public sealed class PartyContactInputValidator : AbstractValidator<PartyContactInput>
{
    public PartyContactInputValidator()
    {
        RuleFor(c => c.Email).NotEmpty().WithMessage("Cada persona de contacto requiere un correo.")
            .EmailAddress().WithMessage("El correo de una persona de contacto no es válido.")
            .MaximumLength(256);
        RuleFor(c => c.Cell).NotEmpty().WithMessage("Cada persona de contacto requiere un celular.")
            .MaximumLength(64)
            .Must(FormValidationRules.IsValidPhone)
            .WithMessage("El celular no es válido (10 dígitos iniciando en 3, o formato internacional +57…).");
        RuleFor(c => c.Phone).Must(FormValidationRules.IsValidPhone)
            .WithMessage("El teléfono no es válido.")
            .When(c => !string.IsNullOrWhiteSpace(c.Phone));
        RuleFor(c => c.FirstName).MaximumLength(FormValidationRules.NameMaxLength)
            .Must(FormValidationRules.IsValidPersonName).WithMessage("Los nombres del contacto contienen caracteres no válidos.")
            .When(c => !string.IsNullOrWhiteSpace(c.FirstName));
        RuleFor(c => c.LastName).MaximumLength(FormValidationRules.NameMaxLength)
            .Must(FormValidationRules.IsValidPersonName).WithMessage("Los apellidos del contacto contienen caracteres no válidos.")
            .When(c => !string.IsNullOrWhiteSpace(c.LastName));
        RuleFor(c => c).Must(c => FormValidationRules.BirthDateError(c.BirthDate) is null)
            .WithMessage(c => FormValidationRules.BirthDateError(c.BirthDate))
            .WithName(nameof(PartyContactInput.BirthDate))
            .When(c => c.BirthDate.HasValue);
    }
}

public sealed class PartyChannelInputValidator : AbstractValidator<PartyChannelInput>
{
    public PartyChannelInputValidator()
    {
        RuleFor(c => c.ChannelTypeCode).NotEmpty().WithMessage("Cada canal de contacto requiere un tipo.").MaximumLength(64);
        RuleFor(c => c.Value).NotEmpty().WithMessage("Cada canal de contacto requiere un valor.").MaximumLength(256);
        RuleFor(c => c).Must(c => FormValidationRules.IsValidChannelValue(c.ChannelTypeCode, c.Value))
            .WithMessage("El valor del canal no tiene el formato esperado para su tipo.")
            .WithName(nameof(PartyChannelInput.Value));
    }
}
