using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.UpdatePriceListItem;

namespace FSH.Modules.Catalog.Features.v1.PriceLists.UpdatePriceListItem;

public sealed class UpdatePriceListItemCommandValidator : AbstractValidator<UpdatePriceListItemCommand>
{
    public UpdatePriceListItemCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ChangeReason).MaximumLength(256).When(x => x.ChangeReason is not null);
        RuleFor(x => x.SalePrice).GreaterThanOrEqualTo(0).When(x => x.SalePrice.HasValue);
    }
}
