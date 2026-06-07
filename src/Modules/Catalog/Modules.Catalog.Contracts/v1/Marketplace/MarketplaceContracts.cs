using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Marketplaces;

// ── Shared refs ──────────────────────────────────────────────────────────
public sealed record AttributeRef(Guid Id, string Name);

// ── Category requirements (read) ─────────────────────────────────────────
public sealed record MarketplaceRequirementGroup(Marketplace Marketplace, IReadOnlyList<Guid> AttributeIds);

public sealed record CategoryRequirementsDto(
    Guid CategoryId,
    IReadOnlyList<MarketplaceRequirementGroup> Groups);

public sealed record GetCategoryRequirementsQuery(Guid CategoryId) : IQuery<CategoryRequirementsDto>;

// ── Category requirements (write — replaces one marketplace's set) ───────
public sealed record SetCategoryRequirementsCommand(
    Guid                CategoryId,
    Marketplace         Marketplace,
    IReadOnlyList<Guid> AttributeIds) : ICommand<int>;

// ── Product validation (non-blocking) ────────────────────────────────────
public sealed record MarketplaceValidationGroup(
    Marketplace               Marketplace,
    IReadOnlyList<AttributeRef> Required,
    IReadOnlyList<AttributeRef> Missing);

public sealed record ProductMarketplaceValidationDto(
    Guid ProductId,
    IReadOnlyList<MarketplaceValidationGroup> Marketplaces);

public sealed record GetProductMarketplaceValidationQuery(Guid ProductId)
    : IQuery<ProductMarketplaceValidationDto>;

// ── Category coverage report (audit) ─────────────────────────────────────
public sealed record CoverageProductRow(Guid ProductId, string ProductName, IReadOnlyList<Guid> CoveredAttributeIds);
public sealed record CoverageAttributeSummary(Guid AttributeId, string Name, int Covered, int Total);

public sealed record CategoryCoverageReportDto(
    Guid                              CategoryId,
    Marketplace?                      Marketplace,
    IReadOnlyList<AttributeRef>       Attributes,
    IReadOnlyList<CoverageProductRow> Products,
    IReadOnlyList<CoverageAttributeSummary> Summary);

public sealed record GetCategoryCoverageReportQuery(Guid CategoryId, Marketplace? Marketplace = null)
    : IQuery<CategoryCoverageReportDto>;
