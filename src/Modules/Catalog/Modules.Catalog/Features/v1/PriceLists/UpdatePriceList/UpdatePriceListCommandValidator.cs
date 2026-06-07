using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.UpdatePriceList;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.UpdatePriceList;

public sealed class UpdatePriceListCommandValidator : AbstractValidator<UpdatePriceListCommand>
{
    public UpdatePriceListCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.AdjustmentPercent)
            .InclusiveBetween(-100m, 1000m)
            .When(x => x.AdjustmentPercent.HasValue);
        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom)
            .When(x => x.ValidTo.HasValue);
        // A default list never carries a %.
        RuleFor(x => x.AdjustmentPercent)
            .Null()
            .When(x => x.IsDefault)
            .WithMessage("La lista por defecto no lleva porcentaje.");
    }
}
