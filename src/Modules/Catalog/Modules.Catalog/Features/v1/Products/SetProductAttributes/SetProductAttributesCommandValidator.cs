using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductAttributes;

namespace FSH.Modules.Catalog.Features.v1.Products.SetProductAttributes;

public sealed class SetProductAttributesCommandValidator : AbstractValidator<SetProductAttributesCommand>
{
    public SetProductAttributesCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Attributes).NotNull();

        RuleForEach(x => x.Attributes).ChildRules(a =>
        {
            a.RuleFor(x => x.AttributeId).NotEmpty();
            a.RuleFor(x => x.ValueIds).NotNull();
            // An attribute that drives variations must select at least one value.
            a.RuleFor(x => x.ValueIds)
                .Must(ids => ids is { Count: > 0 })
                .When(x => x.IsUsedForVariations)
                .WithMessage("Un atributo usado para variaciones debe tener al menos un valor seleccionado.");
        });
    }
}
