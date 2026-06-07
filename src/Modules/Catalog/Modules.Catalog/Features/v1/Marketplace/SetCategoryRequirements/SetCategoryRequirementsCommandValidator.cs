using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Marketplaces;

namespace FSH.Modules.Catalog.Features.v1.Marketplaces.SetCategoryRequirements;

public sealed class SetCategoryRequirementsCommandValidator : AbstractValidator<SetCategoryRequirementsCommand>
{
    public SetCategoryRequirementsCommandValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.AttributeIds).NotNull();
        RuleForEach(x => x.AttributeIds).NotEmpty();
    }
}
