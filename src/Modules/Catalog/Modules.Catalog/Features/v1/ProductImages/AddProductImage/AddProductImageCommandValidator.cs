using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.AddProductImage;

namespace FSH.Modules.Catalog.Features.v1.ProductImages.AddProductImage;

public sealed class AddProductImageCommandValidator : AbstractValidator<AddProductImageCommand>
{
    public AddProductImageCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();

        // Accept absolute URLs (S3/CDN) and root-relative paths (local storage
        // returns "/tenants/{tenant}/product/...", served by the API).
        RuleFor(x => x.Url)
            .NotEmpty()
            .MaximumLength(512)
            .Must(url => !string.IsNullOrWhiteSpace(url)
                && (url.StartsWith('/') || Uri.TryCreate(url, UriKind.Absolute, out _)))
            .WithMessage("Url debe ser una URL absoluta o una ruta relativa válida.");

        RuleFor(x => x.AltText)
            .MaximumLength(256)
            .When(x => x.AltText is not null);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
