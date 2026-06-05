using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.RemoveProductCode;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.RemoveProductCode;

public sealed class RemoveProductCodeCommandValidator : AbstractValidator<RemoveProductCodeCommand>
{
    public RemoveProductCodeCommandValidator()
    {
        RuleFor(x => x.CodeId).NotEmpty();
    }
}
