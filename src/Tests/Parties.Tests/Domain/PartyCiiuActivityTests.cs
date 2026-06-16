using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain;

namespace Parties.Tests.Domain;

/// <summary>
/// PR-D2: la invariante "exactamente una principal" se movió del helper transitorio
/// <c>CiiuActivities.SetPrincipal</c> (PR-A, removido) al agregado <see cref="Party"/>.
/// </summary>
public class PartyCiiuActivityTests
{
    private static Party NewParty() =>
        Party.Create("CC", "1234567", null, PartyKind.Natural, "Tercero CIIU", PartyRole.Customer);

    [Fact]
    public void SetPrincipalCiiu_UnmarksPrevious_LeavesExactlyOnePrincipal()
    {
        var party = NewParty();
        var a = party.AddCiiuActivity("4651", isPrincipal: true);
        var b = party.AddCiiuActivity("4652");
        var c = party.AddCiiuActivity("4653");

        party.SetPrincipalCiiu(b.Id);

        a.IsPrincipal.ShouldBeFalse();
        b.IsPrincipal.ShouldBeTrue();
        c.IsPrincipal.ShouldBeFalse();
        party.CiiuActivities.Count(x => x.IsPrincipal).ShouldBe(1);
    }

    [Fact]
    public void SetPrincipalCiiu_UnknownId_Throws()
    {
        var party = NewParty();
        party.AddCiiuActivity("4651");
        Should.Throw<ArgumentException>(() => party.SetPrincipalCiiu(Guid.CreateVersion7()));
    }

    [Fact]
    public void AddCiiuActivity_Principal_UnmarksPreviousPrincipal()
    {
        var party = NewParty();
        var first = party.AddCiiuActivity("4651", isPrincipal: true);
        var second = party.AddCiiuActivity("4652", isPrincipal: true);

        first.IsPrincipal.ShouldBeFalse();
        second.IsPrincipal.ShouldBeTrue();
        party.CiiuActivities.Count(x => x.IsPrincipal).ShouldBe(1);
    }
}
