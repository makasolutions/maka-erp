using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.DeleteTaxRate;

namespace FSH.Modules.Catalog.Features.v1.TaxRates.DeleteTaxRate;

public sealed class DeleteTaxRateCommandValidator : AbstractValidator<DeleteTaxRateCommand>
{
    public DeleteTaxRateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
