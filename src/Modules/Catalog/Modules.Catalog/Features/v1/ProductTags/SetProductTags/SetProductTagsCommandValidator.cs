using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductTags.SetProductTags;

namespace FSH.Modules.Catalog.Features.v1.ProductTags.SetProductTags;

public sealed class SetProductTagsCommandValidator : AbstractValidator<SetProductTagsCommand>
{
    public SetProductTagsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Tags).NotNull();

        RuleForEach(x => x.Tags).ChildRules(t =>
        {
            t.RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(64);

            t.RuleFor(x => x.Color)
                .Matches(@"^#([0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$")
                .When(x => !string.IsNullOrWhiteSpace(x.Color))
                .WithMessage("Color debe ser un valor hex (#RRGGBB o #RRGGBBAA).");
        });
    }
}
