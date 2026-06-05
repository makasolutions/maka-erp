using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Attributes.AddAttributeValue;

namespace FSH.Modules.Catalog.Features.v1.Attributes.AddAttributeValue;

public sealed class AddAttributeValueCommandValidator : AbstractValidator<AddAttributeValueCommand>
{
    public AddAttributeValueCommandValidator()
    {
        RuleFor(x => x.AttributeId).NotEmpty();

        RuleFor(x => x.Value)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.ColorCode)
            .Matches(@"^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorCode))
            .WithMessage("ColorCode debe ser un valor hex (#RRGGBB o #RRGGBBAA).");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(512)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("ImageUrl debe ser una URL absoluta válida.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
