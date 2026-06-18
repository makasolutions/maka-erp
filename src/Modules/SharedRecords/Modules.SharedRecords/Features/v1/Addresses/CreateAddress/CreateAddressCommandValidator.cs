using FluentValidation;
using FSH.Modules.SharedRecords.Contracts.v1.Addresses.CreateAddress;

namespace FSH.Modules.SharedRecords.Features.v1.Addresses.CreateAddress;

public sealed class CreateAddressCommandValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressCommandValidator()
    {
        RuleFor(x => x.OwnerType).NotEmpty().MaximumLength(64);
        RuleFor(x => x.OwnerId).NotEmpty();
        RuleFor(x => x.Country).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Line).NotEmpty().MaximumLength(256).WithMessage("La dirección es obligatoria.");
        RuleFor(x => x.LabelCode).MaximumLength(64);
        RuleFor(x => x.Department).MaximumLength(128);
        RuleFor(x => x.City).MaximumLength(128);
        RuleFor(x => x.DepartmentCode).MaximumLength(8);
        RuleFor(x => x.MunicipalityCode).MaximumLength(8);
        RuleFor(x => x.Barrio).MaximumLength(128);
        RuleFor(x => x.Reference).MaximumLength(256);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m).When(x => x.Longitude.HasValue);
    }
}
