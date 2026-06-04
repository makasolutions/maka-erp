using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Categories.ListTrashedCategories;

namespace FSH.Modules.Catalog.Features.v1.Categories.ListTrashedCategories;

public sealed class ListTrashedCategoriesValidator : AbstractValidator<ListTrashedCategoriesQuery>
{
    public ListTrashedCategoriesValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .When(x => x.PageNumber.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .When(x => x.PageSize.HasValue);
    }
}
