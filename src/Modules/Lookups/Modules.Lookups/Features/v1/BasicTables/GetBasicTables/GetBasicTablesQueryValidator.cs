using FluentValidation;
using FSH.Modules.Lookups.Contracts.v1.BasicTables.GetBasicTables;

namespace FSH.Modules.Lookups.Features.v1.BasicTables.GetBasicTables;

public sealed class GetBasicTablesQueryValidator : AbstractValidator<GetBasicTablesQuery>
{
    public GetBasicTablesQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThanOrEqualTo(1).When(x => x.PageNumber.HasValue);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200).When(x => x.PageSize.HasValue);
    }
}
