using FluentValidation;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.CreateProduct;

namespace FSH.Modules.Catalog.Features.v1.Products.CreateProduct;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .MaximumLength(220)
            .Matches(@"^[a-z0-9\-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug solo puede contener letras minúsculas, números y guiones.");

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500)
            .When(x => x.ShortDescription is not null);

        RuleFor(x => x.DefaultSku)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.DefaultSku))
            .Must(sku => !string.IsNullOrWhiteSpace(sku))
            .When(x => x.Type is ProductType.Simple or ProductType.Service && !string.IsNullOrWhiteSpace(x.DefaultSku));
    }
}
