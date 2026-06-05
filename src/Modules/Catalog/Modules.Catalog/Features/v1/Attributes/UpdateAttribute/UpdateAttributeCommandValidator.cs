using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Attributes.UpdateAttribute;

namespace FSH.Modules.Catalog.Features.v1.Attributes.UpdateAttribute;

public sealed class UpdateAttributeCommandValidator : AbstractValidator<UpdateAttributeCommand>
{
    public UpdateAttributeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Type)
            .IsInEnum();

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);
    }
}
