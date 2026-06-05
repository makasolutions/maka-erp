using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.UpdateShippingClass;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.UpdateShippingClass;

public sealed class UpdateShippingClassCommandValidator : AbstractValidator<UpdateShippingClassCommand>
{
    public UpdateShippingClassCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).MaximumLength(256).When(x => x.Description is not null);
    }
}
