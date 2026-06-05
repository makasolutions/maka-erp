using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Attributes.RemoveAttributeValue;

namespace FSH.Modules.Catalog.Features.v1.Attributes.RemoveAttributeValue;

public sealed class RemoveAttributeValueCommandValidator : AbstractValidator<RemoveAttributeValueCommand>
{
    public RemoveAttributeValueCommandValidator()
    {
        RuleFor(x => x.AttributeId).NotEmpty();
        RuleFor(x => x.ValueId).NotEmpty();
    }
}
