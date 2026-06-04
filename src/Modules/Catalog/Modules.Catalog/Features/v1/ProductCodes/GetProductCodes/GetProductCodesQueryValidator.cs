using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.GetProductCodes;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.GetProductCodes;

public sealed class GetProductCodesQueryValidator : AbstractValidator<GetProductCodesQuery>
{
    public GetProductCodesQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariationId).NotEmpty();
    }
}
