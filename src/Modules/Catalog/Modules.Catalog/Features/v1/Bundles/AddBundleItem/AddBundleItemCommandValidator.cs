using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Bundles.AddBundleItem;

namespace FSH.Modules.Catalog.Features.v1.Bundles.AddBundleItem;

public sealed class AddBundleItemCommandValidator : AbstractValidator<AddBundleItemCommand>
{
    public AddBundleItemCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ItemVariationId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.DiscountPercent)
            .InclusiveBetween(0m, 100m)
            .When(x => x.DiscountPercent.HasValue);
        RuleFor(x => x.DiscountFixed)
            .GreaterThanOrEqualTo(0m)
            .When(x => x.DiscountFixed.HasValue);
        RuleFor(x => x.SortOrder).GreaterThanOrEqualTo(0);
    }
}
