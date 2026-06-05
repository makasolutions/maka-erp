using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Bundles.RemoveBundleItem;

namespace FSH.Modules.Catalog.Features.v1.Bundles.RemoveBundleItem;

public sealed class RemoveBundleItemCommandValidator : AbstractValidator<RemoveBundleItemCommand>
{
    public RemoveBundleItemCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
    }
}
