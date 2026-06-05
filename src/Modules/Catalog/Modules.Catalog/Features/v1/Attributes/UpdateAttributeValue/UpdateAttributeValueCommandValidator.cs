using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttributeValue;

namespace FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttributeValue;

public sealed class UpdateAttributeValueCommandValidator : AbstractValidator<UpdateAttributeValueCommand>
{
    public UpdateAttributeValueCommandValidator()
    {
        RuleFor(x => x.AttributeId).NotEmpty();
        RuleFor(x => x.ValueId).NotEmpty();

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
