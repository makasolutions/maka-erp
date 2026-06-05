using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Variations.DeleteVariation;

namespace FSH.Modules.Catalog.Features.v1.Variations.DeleteVariation;

public sealed class DeleteVariationCommandValidator : AbstractValidator<DeleteVariationCommand>
{
    public DeleteVariationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
