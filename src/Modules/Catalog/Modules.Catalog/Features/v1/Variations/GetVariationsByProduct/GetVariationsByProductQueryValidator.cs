using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Variations.GetVariationsByProduct;

namespace FSH.Modules.Catalog.Features.v1.Variations.GetVariationsByProduct;

public sealed class GetVariationsByProductQueryValidator : AbstractValidator<GetVariationsByProductQuery>
{
    public GetVariationsByProductQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
