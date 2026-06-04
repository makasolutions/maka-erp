using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.AddPriceListItem;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.AddPriceListItem;

public sealed class AddPriceListItemCommandValidator : AbstractValidator<AddPriceListItemCommand>
{
    public AddPriceListItemCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.VariationId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinQuantity).GreaterThan(0).When(x => x.MinQuantity.HasValue);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
        RuleFor(x => x.SalePriceTo)
            .GreaterThan(x => x.SalePriceFrom!.Value)
            .When(x => x.SalePriceFrom.HasValue && x.SalePriceTo.HasValue)
            .WithMessage("SalePriceTo debe ser posterior a SalePriceFrom.");
    }
}
