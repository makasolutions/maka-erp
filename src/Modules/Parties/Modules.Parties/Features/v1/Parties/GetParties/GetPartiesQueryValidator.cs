using FluentValidation;
using FSH.Modules.Parties.Contracts.v1.Parties.GetParties;

namespace FSH.Modules.Parties.Features.v1.Parties.GetParties;

public sealed class GetPartiesQueryValidator : AbstractValidator<GetPartiesQuery>
{
    public GetPartiesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).When(x => x.PageSize.HasValue);
    }
}
