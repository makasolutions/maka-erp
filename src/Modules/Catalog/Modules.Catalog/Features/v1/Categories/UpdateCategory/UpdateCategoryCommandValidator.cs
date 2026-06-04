using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Categories.UpdateCategory;

namespace FSH.Modules.Catalog.Features.v1.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

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

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("ImageUrl debe ser una URL absoluta válida.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
