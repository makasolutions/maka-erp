using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.ListTrashedProducts;

namespace FSH.Modules.Catalog.Features.v1.Products.ListTrashedProducts;

public sealed class ListTrashedProductsValidator : AbstractValidator<ListTrashedProductsQuery>
{
    public ListTrashedProductsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .When(x => x.PageNumber.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .When(x => x.PageSize.HasValue);
    }
}
