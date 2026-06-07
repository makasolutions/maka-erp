using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class CatalogPricingTests
{
    [Theory]
    [InlineData(7_000_000, 15, 8_049_000)]   // +15% → 8.050.000 → −1.000
    [InlineData(7_000_000, -3, 6_789_000)]   // −3%  → 6.790.000 → −1.000
    [InlineData(1_000_000, 0, 999_000)]      // 0%   → 1.000.000 → −1.000
    public void Derive_Should_ApplyPercent_AndRound(decimal basePrice, decimal pct, decimal expected)
    {
        CatalogPricing.Derive(basePrice, pct).ShouldBe(expected);
    }

    [Fact]
    public void RoundSuggested_Should_NeverGoNegative()
    {
        CatalogPricing.RoundSuggested(500).ShouldBe(0);
        CatalogPricing.RoundSuggested(0).ShouldBe(0);
    }

    [Fact]
    public void ApplyDerived_Should_RespectManualOverride()
    {
        var item = PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), 6_789_000, "u1");
        item.SetManualOverride(true);
        item.ApplyDerived(5_000_000, "u1");
        item.Price.ShouldBe(6_789_000);  // unchanged
    }

    [Fact]
    public void ApplyDerived_Should_UpdateNonOverride_AndRecordHistory()
    {
        var item = PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), 6_789_000, "u1");
        item.ApplyDerived(8_049_000, "u1");
        item.Price.ShouldBe(8_049_000);
        item.IsManualOverride.ShouldBeFalse();
        item.History.Count.ShouldBe(1);
    }
}

public sealed class PriceListDefaultTests
{
    [Fact]
    public void MakeDefault_Should_ClearPercent()
    {
        var list = PriceList.Create("Mayoristas", "mayoristas", DateTime.UtcNow, adjustmentPercent: -3);
        list.AdjustmentPercent.ShouldBe(-3);
        list.MakeDefault();
        list.IsDefault.ShouldBeTrue();
        list.AdjustmentPercent.ShouldBeNull();
    }
}
