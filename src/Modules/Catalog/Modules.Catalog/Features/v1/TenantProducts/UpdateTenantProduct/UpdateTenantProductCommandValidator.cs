using FluentValidation;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.UpdateTenantProduct;

namespace FSH.Modules.Catalog.Features.v1.TenantProducts.UpdateTenantProduct;

public sealed class UpdateTenantProductCommandValidator : AbstractValidator<UpdateTenantProductCommand>
{
    public UpdateTenantProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameOverride).MaximumLength(200).When(x => x.NameOverride is not null);
        RuleFor(x => x.ShortDescriptionOverride).MaximumLength(500).When(x => x.ShortDescriptionOverride is not null);
        RuleFor(x => x.DropshippingPrice).GreaterThanOrEqualTo(0m).When(x => x.DropshippingPrice.HasValue);
        RuleFor(x => x.DropshippingMinQty).GreaterThanOrEqualTo(0m).When(x => x.DropshippingMinQty.HasValue);
    }
}
