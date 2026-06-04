using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.BulkUpdatePrices;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.BulkUpdatePrices;

public sealed class BulkUpdatePricesFromCsvCommandValidator : AbstractValidator<BulkUpdatePricesFromCsvCommand>
{
    public BulkUpdatePricesFromCsvCommandValidator()
    {
        RuleFor(x => x.PriceListId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.CsvContent).NotEmpty();
        RuleFor(x => x.SupplierCodeColumn).NotEmpty().MaximumLength(128);
        RuleFor(x => x.PriceColumn).NotEmpty().MaximumLength(128);
        RuleFor(x => x.ChangeReason).MaximumLength(256).When(x => x.ChangeReason is not null);
    }
}
