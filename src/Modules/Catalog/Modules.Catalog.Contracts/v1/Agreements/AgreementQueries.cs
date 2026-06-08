using FSH.Framework.Shared.Persistence;
using FSH.Modules.Catalog.Contracts.Enums;
using Mediator;

namespace FSH.Modules.Catalog.Contracts.v1.Agreements;

public sealed record GetAgreementsQuery : IPagedQuery, IQuery<PagedResponse<AgreementDto>>
{
    public int?             PageNumber { get; set; } = 1;
    public int?             PageSize   { get; set; } = 50;
    public string?          Sort       { get; set; }
    public string?          Search     { get; set; }
    public Guid?            SupplierId { get; set; }
    public AgreementStatus? Status     { get; set; }
}

public sealed record GetAgreementByIdQuery(Guid Id) : IQuery<AgreementDetailDto>;

public sealed record EvaluateAgreementQuery(Guid AgreementId, Guid DistributorPartyId)
    : IQuery<AgreementEvaluationDto>;
