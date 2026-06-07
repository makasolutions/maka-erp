using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.DuplicateProduct;

namespace FSH.Modules.Catalog.Features.v1.Products.DuplicateProduct;

public sealed class DuplicateProductCommandValidator : AbstractValidator<DuplicateProductCommand>
{
    public DuplicateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
