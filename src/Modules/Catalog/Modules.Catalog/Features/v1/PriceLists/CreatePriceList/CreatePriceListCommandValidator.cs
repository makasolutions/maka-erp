using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.CreatePriceList;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.CreatePriceList;

public sealed class CreatePriceListCommandValidator : AbstractValidator<CreatePriceListCommand>
{
    public CreatePriceListCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Description)
            .MaximumLength(512)
            .When(x => x.Description is not null);

        // Free-form segment (Retail, Mayorista, MercadoLibre…) — lowercased in the domain.
        RuleFor(x => x.CustomerSegment)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.AdjustmentPercent)
            .InclusiveBetween(-100m, 1000m)
            .When(x => x.AdjustmentPercent.HasValue);

        // The default list never carries a %.
        RuleFor(x => x.AdjustmentPercent)
            .Null()
            .When(x => x.IsDefault)
            .WithMessage("La lista por defecto no lleva porcentaje.");

        RuleFor(x => x.ValidTo)
            .GreaterThan(x => x.ValidFrom!.Value)
            .When(x => x.ValidTo.HasValue && x.ValidFrom.HasValue);
    }
}
