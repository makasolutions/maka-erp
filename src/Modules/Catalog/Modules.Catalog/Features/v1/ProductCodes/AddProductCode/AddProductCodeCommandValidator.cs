using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.AddProductCode;

namespace FSH.Modules.Catalog.Features.v1.ProductCodes.AddProductCode;

public sealed class AddProductCodeCommandValidator : AbstractValidator<AddProductCodeCommand>
{
    public AddProductCodeCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.VariationId).NotEmpty();

        RuleFor(x => x.CodeType)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(128);

        // SupplierId is required when CodeType is SupplierCode
        RuleFor(x => x.SupplierId)
            .NotEmpty()
            .When(x => string.Equals(x.CodeType, "SupplierCode", StringComparison.OrdinalIgnoreCase))
            .WithMessage("SupplierId es obligatorio cuando CodeType = 'SupplierCode'.");
    }
}
