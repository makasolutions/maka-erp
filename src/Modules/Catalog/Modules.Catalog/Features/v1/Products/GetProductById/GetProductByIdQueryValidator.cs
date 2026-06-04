using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.GetProductById;

namespace FSH.Modules.Catalog.Features.v1.Products.GetProductById;

public sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
