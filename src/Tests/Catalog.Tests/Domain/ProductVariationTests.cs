using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class ProductVariationTests
{
    [Fact]
    public void Create_Should_UppercaseAndTrimSku()
    {
        var v = ProductVariation.Create(Guid.NewGuid(), "  sony-fx3-kit  ");
        v.Sku.ShouldBe("SONY-FX3-KIT");
    }

    [Fact]
    public void Create_Should_DefaultToActiveManageStock()
    {
        var v = ProductVariation.Create(Guid.NewGuid(), "sku");
        v.IsActive.ShouldBeTrue();
        v.ManageStock.ShouldBeTrue();
        v.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void ClearDefault_Should_RemoveDefaultFlag()
    {
        var v = ProductVariation.Create(Guid.NewGuid(), "sku", isDefault: true);
        v.IsDefault.ShouldBeTrue();
        v.ClearDefault();
        v.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void ClearDefault_Should_BeNoOp_When_NotDefault()
    {
        var v = ProductVariation.Create(Guid.NewGuid(), "sku");
        v.ClearDefault();
        v.IsDefault.ShouldBeFalse();
    }

    [Fact]
    public void Restore_Should_BeNoOp_When_NotDeleted()
    {
        var v = ProductVariation.Create(Guid.NewGuid(), "sku");
        v.Restore();
        v.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_ThrowOnBlankSku()
    {
        Should.Throw<System.ArgumentException>(() => ProductVariation.Create(Guid.NewGuid(), "  "));
    }
}
