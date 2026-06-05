using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Variations.GenerateVariations;

namespace FSH.Modules.Catalog.Features.v1.Variations.GenerateVariations;

public sealed class GenerateVariationsCommandValidator : AbstractValidator<GenerateVariationsCommand>
{
    public GenerateVariationsCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
