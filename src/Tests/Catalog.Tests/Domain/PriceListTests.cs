using System.Linq;
using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class PriceListTests
{
    [Fact]
    public void Create_Should_NormalizeSegmentAndStartActive()
    {
        var list = PriceList.Create("Retail 2026", "RETAIL", DateTime.UtcNow);
        list.CustomerSegment.ShouldBe("retail");
        list.IsActive.ShouldBeTrue();
        list.ValidTo.ShouldBeNull();
    }

    [Fact]
    public void Close_Should_SetValidToAndDeactivate()
    {
        var list = PriceList.Create("Retail", "retail", DateTime.UtcNow);
        var closeAt = DateTime.UtcNow;

        list.Close(closeAt);

        list.ValidTo.ShouldBe(closeAt);
        list.IsActive.ShouldBeFalse();
    }
}

public sealed class PriceListItemTests
{
    [Fact]
    public void ChangePrice_Should_RecordImmutableHistoryEntry()
    {
        var item = PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), 25_000_000m, "user-1");

        item.ChangePrice(23_900_000m, "user-2", "Ajuste promocional");

        item.Price.ShouldBe(23_900_000m);
        item.History.Count.ShouldBe(1);
        var h = item.History.Single();
        h.OldPrice.ShouldBe(25_000_000m);
        h.NewPrice.ShouldBe(23_900_000m);
        h.ChangedByUserId.ShouldBe("user-2");
        h.ChangeReason.ShouldBe("Ajuste promocional");
    }

    [Fact]
    public void ChangePrice_Should_AccumulateHistory_OnMultipleChanges()
    {
        var item = PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, "u");
        item.ChangePrice(90m, "u");
        item.ChangePrice(80m, "u");
        item.History.Count.ShouldBe(2);
        item.Price.ShouldBe(80m);
    }

    [Fact]
    public void Create_Should_ThrowOnNegativePrice()
    {
        Should.Throw<System.ArgumentOutOfRangeException>(
            () => PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), -1m, "u"));
    }
}
