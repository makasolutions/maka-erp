using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.SetPrimaryImage;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.SetPrimaryImage;

public sealed class SetPrimaryImageCommandValidator : AbstractValidator<SetPrimaryImageCommand>
{
    public SetPrimaryImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.ImageId).NotEmpty();
    }
}
