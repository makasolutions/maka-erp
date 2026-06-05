using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.AddProductImage;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.AddProductImage;

public sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(512)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("Url debe ser una URL absoluta válida.");

        RuleFor(x => x.AltText)
            .MaximumLength(256)
            .When(x => x.AltText is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
