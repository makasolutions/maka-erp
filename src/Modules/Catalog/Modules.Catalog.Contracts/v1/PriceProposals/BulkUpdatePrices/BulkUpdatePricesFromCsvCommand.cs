using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.PriceProposals.BulkUpdatePrices;

public sealed record BulkUpdatePricesFromCsvCommand(
    Guid    PriceListId,
    Guid    SupplierId,
    string  CsvContent,
    string  SupplierCodeColumn,
    string  PriceColumn,
    string? ChangeReason     = null,
    string? SourceReference  = null) : ICommand<BulkUpdatePricesResult>;

public sealed record BulkUpdatePricesResult(
    Guid          BatchId,
    int           MatchedCount,
    int           NotFoundCount,
    int           ProposedCount,
    IReadOnlyList<string> NotFoundCodes);
