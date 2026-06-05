using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.RemoveProductImage;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.RemoveProductImage;

public sealed class RemoveProductImageCommandValidator : AbstractValidator<RemoveProductImageCommand>
{
    public RemoveProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}
