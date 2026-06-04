using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Brands.ListTrashedBrands;

namespace FSH.Modules.Catalog.Features.v1.Brands.ListTrashedBrands;

public sealed class ListTrashedBrandsQueryValidator : AbstractValidator<ListTrashedBrandsQuery>
{
    public ListTrashedBrandsQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).When(x => x.PageSize.HasValue);
    }
}
