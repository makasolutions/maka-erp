using FSH.Modules.Catalog.Contracts.v1.TenantProducts.CloneProduct;
using FSH.Modules.Catalog.Contracts.v1.TenantProducts.UpdateTenantProduct;
using FSH.Modules.Catalog.Features.v1.TenantProducts.CloneProduct;
using FSH.Modules.Catalog.Features.v1.TenantProducts.UpdateTenantProduct;

namespace Catalog.Tests.Validators;

public sealed class TenantProductValidatorTests
{
    [Fact]
    public void Clone_RejectsEmptyCanonicalId()
    {
        new CloneProductToTenantCommandValidator()
            .Validate(new CloneProductToTenantCommand(Guid.Empty))
            .IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Clone_AcceptsValid()
    {
        new CloneProductToTenantCommandValidator()
            .Validate(new CloneProductToTenantCommand(Guid.NewGuid()))
            .IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Update_RejectsNegativeDropshippingPrice()
    {
        var cmd = new UpdateTenantProductCommand(
            Guid.NewGuid(), "Override", null, null, null, null, null, -5m, null, true, true);
        new UpdateTenantProductCommandValidator().Validate(cmd).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Update_AcceptsValid()
    {
        var cmd = new UpdateTenantProductCommand(
            Guid.NewGuid(), "Mi nombre", "corta", null, null, null, null, 12000m, 1m, true, false);
        new UpdateTenantProductCommandValidator().Validate(cmd).IsValid.ShouldBeTrue();
    }
}
