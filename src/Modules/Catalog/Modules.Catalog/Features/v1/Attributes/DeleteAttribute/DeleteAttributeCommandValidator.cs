using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.Attributes.DeleteAttribute;

namespace FSH.Modules.Catalog.Features.v1.Attributes.DeleteAttribute;

public sealed class DeleteAttributeCommandValidator : AbstractValidator<DeleteAttributeCommand>
{
    public DeleteAttributeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
