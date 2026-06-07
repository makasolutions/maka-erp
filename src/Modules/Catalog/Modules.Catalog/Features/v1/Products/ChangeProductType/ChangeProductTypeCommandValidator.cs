using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.ChangeProductType;

namespace FSH.Modules.Catalog.Features.v1.Products.ChangeProductType;

public sealed class ChangeProductTypeCommandValidator : AbstractValidator<ChangeProductTypeCommand>
{
    public ChangeProductTypeCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
