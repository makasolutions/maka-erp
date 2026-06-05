using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Attributes.CreateAttribute;

namespace FSH.Modules.Catalog.Features.v1.Attributes.CreateAttribute;

public sealed class CreateAttributeCommandValidator : AbstractValidator<CreateAttributeCommand>
{
    public CreateAttributeCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Slug)
            .MaximumLength(160)
            .Matches(@"^[a-z0-9\-]+$")
            .When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug solo puede contener letras minúsculas, números y guiones.");

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
