using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Brands.GetBrandById;

namespace FSH.Modules.Catalog.Features.v1.Brands.GetBrandById;

public sealed class GetBrandByIdQueryValidator : AbstractValidator<GetBrandByIdQuery>
{
    public GetBrandByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
