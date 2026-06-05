using FluentValidation;
using FSH.Modules.Identity.Contracts.v1.Roles.GetRoles;

namespace FSH.Modules.Identity.Features.v1.Roles.GetRoles;

public sealed class GetRolesQueryValidator : AbstractValidator<GetRolesQuery>
{
    public GetRolesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .When(x => x.PageNumber.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .When(x => x.PageSize.HasValue);
    }
}
