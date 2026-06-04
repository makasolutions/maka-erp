using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.PriceProposals.ApprovePriceProposals;

namespace FSH.Modules.Catalog.Features.v1.PriceProposals.ApprovePriceProposals;

public sealed class ApprovePriceProposalsCommandValidator : AbstractValidator<ApprovePriceProposalsCommand>
{
    public ApprovePriceProposalsCommandValidator()
    {
        RuleFor(x => x.BatchId).NotEmpty();
    }
}
