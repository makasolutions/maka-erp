using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.UpdateTaxRate;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.UpdateTaxRate;

public sealed class UpdateTaxRateCommandValidator : AbstractValidator<UpdateTaxRateCommand>
{
    public UpdateTaxRateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Rate).InclusiveBetween(0m, 1m)
            .WithMessage("Rate debe ser una fracción entre 0 y 1 (ej. 0.19 para 19%).");
        RuleFor(x => x.Description).MaximumLength(256).When(x => x.Description is not null);
    }
}
