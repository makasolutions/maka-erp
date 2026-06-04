using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.RejectPriceProposals;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.RejectPriceProposals;

public sealed class RejectPriceProposalsCommandValidator : AbstractValidator<RejectPriceProposalsCommand>
{
    public RejectPriceProposalsCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
    }
}
