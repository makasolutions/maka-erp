using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.CloneProduct;

namespace FSH.Modules.Catalog.Features.v1.TenantProducts.CloneProduct;

public sealed class CloneProductToTenantCommandValidator : AbstractValidator<CloneProductToTenantCommand>
{
    public CloneProductToTenantCommandValidator()
    {
        RuleFor(x => x.CanonicalProductId).NotEmpty();
    }
}
