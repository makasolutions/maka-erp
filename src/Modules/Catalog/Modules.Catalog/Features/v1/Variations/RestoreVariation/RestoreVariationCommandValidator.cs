using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Variations.RestoreVariation;

namespace FSH.Modules.Catalog.Features.v1.Variations.RestoreVariation;

public sealed class RestoreVariationCommandValidator : AbstractValidator<RestoreVariationCommand>
{
    public RestoreVariationCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
