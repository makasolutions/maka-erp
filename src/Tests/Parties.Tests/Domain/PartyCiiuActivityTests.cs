using FSH.Modules.Parties.Domain.V2;

namespace Parties.Tests.Domain;

public class PartyCiiuActivityTests
{
    [Fact]
    public void SetPrincipal_UnmarksPrevious_LeavesExactlyOnePrincipal()
    {
        var partyId = Guid.CreateVersion7();
        var a = PartyCiiuActivity.Create(partyId, "4651", isPrincipal: true);
        var b = PartyCiiuActivity.Create(partyId, "4652");
        var c = PartyCiiuActivity.Create(partyId, "4653");
        var list = new List<PartyCiiuActivity> { a, b, c };

        CiiuActivities.SetPrincipal(list, b.Id);

        a.IsPrincipal.ShouldBeFalse();
        b.IsPrincipal.ShouldBeTrue();
        c.IsPrincipal.ShouldBeFalse();
        list.Count(x => x.IsPrincipal).ShouldBe(1);
        CiiuActivities.HasValidPrincipal(list).ShouldBeTrue();
    }

    [Fact]
    public void SetPrincipal_UnknownId_Throws()
    {
        var list = new List<PartyCiiuActivity> { PartyCiiuActivity.Create(Guid.CreateVersion7(), "4651") };
        Should.Throw<ArgumentException>(() => CiiuActivities.SetPrincipal(list, Guid.CreateVersion7()));
    }

    [Fact]
    public void HasValidPrincipal_EmptyCollection_IsTrue()
    {
        CiiuActivities.HasValidPrincipal([]).ShouldBeTrue();
    }

    [Fact]
    public void HasValidPrincipal_TwoPrincipals_IsFalse()
    {
        var partyId = Guid.CreateVersion7();
        var list = new List<PartyCiiuActivity>
        {
            PartyCiiuActivity.Create(partyId, "4651", isPrincipal: true),
            PartyCiiuActivity.Create(partyId, "4652", isPrincipal: true),
        };
        CiiuActivities.HasValidPrincipal(list).ShouldBeFalse();
    }
}
