using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class CampaignTests
{
    [Fact]
    public void Create_Campaign_Should_StartScheduled()
    {
        var c = PriceList.Create("Sell-IN Junio", "campaign", DateTime.UtcNow,
            listKind: PriceListKind.Campaign, validTo: DateTime.UtcNow.AddDays(7));

        c.ListKind.ShouldBe(PriceListKind.Campaign);
        c.CampaignStatus.ShouldBe(CampaignStatus.Scheduled);
    }

    [Fact]
    public void Transition_ToEnded_Should_Deactivate()
    {
        var c = PriceList.Create("Sell-OUT", "campaign", DateTime.UtcNow,
            listKind: PriceListKind.Campaign, validTo: DateTime.UtcNow.AddDays(1));
        c.SetCampaignJobs("job-start", "job-end");
        c.StartJobId.ShouldBe("job-start");

        c.TransitionCampaign(CampaignStatus.Running);
        c.CampaignStatus.ShouldBe(CampaignStatus.Running);
        c.IsActive.ShouldBeTrue();

        c.TransitionCampaign(CampaignStatus.Ended);
        c.CampaignStatus.ShouldBe(CampaignStatus.Ended);
        c.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void SnapshotPreCampaign_Should_RememberBasePrice()
    {
        var item = PriceListItem.Create(Guid.NewGuid(), Guid.NewGuid(), 5_000_000, "u1");
        item.SnapshotPreCampaign(7_000_000);
        item.PreCampaignPrice.ShouldBe(7_000_000);
    }
}
