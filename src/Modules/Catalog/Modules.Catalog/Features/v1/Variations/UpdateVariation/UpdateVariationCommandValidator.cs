using FluentValidation;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Variations.UpdateVariation;

namespace FSH.Modules.Catalog.Features.v1.Variations.UpdateVariation;

public sealed class UpdateVariationCommandValidator : AbstractValidator<UpdateVariationCommand>
{
    private static readonly string[] ValidWeightUnits = Enum.GetNames<WeightUnit>();

    public UpdateVariationCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Description)
            .MaximumLength(256)
            .When(x => x.Description is not null);

        RuleFor(x => x.WeightUnit)
            .Must(u => Array.Exists(ValidWeightUnits, w => string.Equals(w, u, StringComparison.OrdinalIgnoreCase)))
            .When(x => !string.IsNullOrWhiteSpace(x.WeightUnit))
            .WithMessage($"WeightUnit debe ser uno de: {string.Join(", ", ValidWeightUnits)}");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(512)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out _))
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
            .WithMessage("ImageUrl debe ser una URL absoluta válida.");

        RuleFor(x => x.LowStockThreshold)
            .GreaterThanOrEqualTo(0)
            .When(x => x.LowStockThreshold.HasValue);
    }
}
