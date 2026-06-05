using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.PublishProduct;

namespace FSH.Modules.Catalog.Features.v1.Products.PublishProduct;

public sealed class PublishProductCommandValidator : AbstractValidator<PublishProductCommand>
{
    public PublishProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
