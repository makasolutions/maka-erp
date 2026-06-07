using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class CatalogPricingTests
{
    [Fact]
    public void TaxRoundTrip_Should_Match()
    {
        CatalogPricing.ToTaxIncluded(7_000_000).ShouldBe(8_330_000);
        CatalogPricing.FromTaxIncluded(8_330_000).ShouldBe(7_000_000);
    }

    [Theory]
    // base default 7.000.000 → IVA incl. 8.330.000
    [InlineData(7_000_000, 15, 9_579_000)]   // ×1.15 = 9.579.500 → round → 9.579.000
    [InlineData(7_000_000, -3, 8_079_000)]   // ×0.97 = 8.080.100 → round → 8.079.000
    public void DeriveBase_WithRound_TaxIncluded_IsRounded(decimal baseDefault, decimal pct, decimal expectedTaxIncl)
    {
        var derivedBase = CatalogPricing.DeriveBase(baseDefault, pct, round: true);
        Math.Round(CatalogPricing.ToTaxIncluded(derivedBase)).ShouldBe(expectedTaxIncl);
    }

    [Fact]
    public void DeriveBase_NoRound_KeepsExactTaxIncluded()
    {
        var derivedBase = CatalogPricing.DeriveBase(7_000_000, 15, round: false);
        // 8.330.000 × 1.15 = 9.579.500 (no rounding)
        Math.Round(CatalogPricing.ToTaxIncluded(derivedBase)).ShouldBe(9_579_500);
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
