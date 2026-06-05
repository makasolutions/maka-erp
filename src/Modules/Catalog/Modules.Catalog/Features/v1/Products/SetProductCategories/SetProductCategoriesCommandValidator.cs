using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductCategories;

namespace FSH.Modules.Catalog.Features.v1.Products.SetProductCategories;

public sealed class SetProductCategoriesCommandValidator : AbstractValidator<SetProductCategoriesCommand>
{
    public SetProductCategoriesCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Categories).NotNull();

        RuleForEach(x => x.Categories)
            .ChildRules(c => c.RuleFor(a => a.CategoryId).NotEmpty());

        RuleFor(x => x.Categories)
            .Must(cs => cs.Count(c => c.IsPrimary) <= 1)
            .WithMessage("Solo una categoría puede ser la principal.");
    }
}
