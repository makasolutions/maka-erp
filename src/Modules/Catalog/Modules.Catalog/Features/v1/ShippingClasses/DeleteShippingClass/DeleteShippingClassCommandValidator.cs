using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.DeleteShippingClass;

namespace FSH.Modules.Catalog.Features.v1.ShippingClasses.DeleteShippingClass;

public sealed class DeleteShippingClassCommandValidator : AbstractValidator<DeleteShippingClassCommand>
{
    public DeleteShippingClassCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
