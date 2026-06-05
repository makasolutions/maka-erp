using FSH.Modules.Catalog.Contracts.v1.Bundles.AddBundleItem;
using FSH.Modules.Catalog.Features.v1.Bundles.AddBundleItem;

namespace Catalog.Tests.Validators;

public sealed class BundleValidatorTests
{
    private static AddBundleItemCommand Cmd(int qty = 1, decimal? pct = null) =>
        new(Guid.NewGuid(), Guid.NewGuid(), qty, pct);

    [Fact]
    public void Accepts_Valid()
    {
        new AddBundleItemCommandValidator().Validate(Cmd(2, 10m)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Rejects_ZeroQuantity()
    {
        new AddBundleItemCommandValidator().Validate(Cmd(0)).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(150)]
    [InlineData(-5)]
    public void Rejects_DiscountPercentOutOfRange(int pct)
    {
        new AddBundleItemCommandValidator().Validate(Cmd(1, pct)).IsValid.ShouldBeFalse();
    }
}
