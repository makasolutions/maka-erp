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

// PR-2: PartyContactInputValidator ELIMINADO (PartyContact migró a PartyRelationship).

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
