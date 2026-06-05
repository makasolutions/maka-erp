using System.Text.Json;
using FluentValidation;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Products.UpdateProduct;

namespace FSH.Modules.Catalog.Features.v1.Products.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private static readonly string[] ValidWeightUnits    = Enum.GetNames<WeightUnit>();
    private static readonly string[] ValidDimensionUnits = Enum.GetNames<DimensionUnit>();

    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ShortDescription)
            .MaximumLength(500)
            .When(x => x.ShortDescription is not null);

        RuleFor(x => x.Description)
            .MaximumLength(50_000)
            .When(x => x.Description is not null);

        RuleFor(x => x.TechnicalSpecs)
            .MaximumLength(10_000)
            .When(x => x.TechnicalSpecs is not null);

        RuleFor(x => x.SeoTitle)
            .MaximumLength(60)
            .When(x => x.SeoTitle is not null);

        RuleFor(x => x.SeoDescription)
            .MaximumLength(160)
            .When(x => x.SeoDescription is not null);

        RuleFor(x => x.WeightUnit)
            .Must(u => Array.Exists(ValidWeightUnits, w => string.Equals(w, u, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"WeightUnit debe ser uno de: {string.Join(", ", ValidWeightUnits)}");

        RuleFor(x => x.DimensionUnit)
            .Must(u => Array.Exists(ValidDimensionUnits, d => string.Equals(d, u, StringComparison.OrdinalIgnoreCase)))
            .WithMessage($"DimensionUnit debe ser uno de: {string.Join(", ", ValidDimensionUnits)}");

        RuleFor(x => x.Specs)
            .Must(BeValidJson)
            .When(x => !string.IsNullOrWhiteSpace(x.Specs))
            .WithMessage("Specs debe ser un JSON válido.");
    }

    private static bool BeValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        try
        {
            using var _ = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
