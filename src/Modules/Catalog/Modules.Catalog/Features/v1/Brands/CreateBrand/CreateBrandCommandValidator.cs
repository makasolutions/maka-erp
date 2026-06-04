using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Brands.CreateBrand;

namespace FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;

public sealed class CreateBrandCommandValidator : AbstractValidator<CreateBrandCommand>
{
    public CreateBrandCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .MaximumLength(200)
            .Matches(@"^[a-z0-9\-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug solo puede contener letras minúsculas, números y guiones.");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => x.Description is not null);

        RuleFor(x => x.LogoUrl)
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.LogoUrl))
            .WithMessage("LogoUrl debe ser una URL absoluta válida.");

        RuleFor(x => x.WebsiteUrl)
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.WebsiteUrl))
            .WithMessage("WebsiteUrl debe ser una URL absoluta válida.");

        RuleFor(x => x.CountryOfOrigin)
            .Length(2)
            .Matches(@"^[A-Z]{2}$")
            .When(x => !string.IsNullOrWhiteSpace(x.CountryOfOrigin))
            .WithMessage("CountryOfOrigin debe ser un código ISO-3166-1 alpha-2 en mayúsculas (ej. CO, JP).");
    }
}
