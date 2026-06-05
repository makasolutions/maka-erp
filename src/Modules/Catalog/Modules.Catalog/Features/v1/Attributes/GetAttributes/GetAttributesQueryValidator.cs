using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Attributes.GetAttributes;

namespace FSH.Modules.Catalog.Features.v1.Attributes.GetAttributes;

public sealed class GetAttributesQueryValidator : AbstractValidator<GetAttributesQuery>
{
    public GetAttributesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .When(x => x.PageNumber.HasValue);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 200)
            .When(x => x.PageSize.HasValue);
    }
}
