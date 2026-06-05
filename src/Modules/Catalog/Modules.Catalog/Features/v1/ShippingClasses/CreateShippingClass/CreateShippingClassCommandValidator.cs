using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.CreateShippingClass;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.CreateShippingClass;

public sealed class CreateShippingClassCommandValidator : AbstractValidator<CreateShippingClassCommand>
{
    public CreateShippingClassCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Description).MaximumLength(256).When(x => x.Description is not null);
    }
}
