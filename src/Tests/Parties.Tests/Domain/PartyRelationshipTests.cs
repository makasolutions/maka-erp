using System.Text.Json;
using FSH.Modules.Parties.Domain.Relationships;

namespace Parties.Tests.Domain;

/// <summary>
/// PR-2: entidad M2M PartyRelationship. Vigencia Start/End, auto-vínculo prohibido, baja lógica.
/// El invariante "un principal por empresa" se valida en el handler + índice parcial (integration).
/// </summary>
public class PartyRelationshipTests
{
    private static readonly Guid Persona  = Guid.NewGuid();
    private static readonly Guid EmpresaA = Guid.NewGuid();
    private static readonly Guid EmpresaB = Guid.NewGuid();

    [Fact]
    public void Create_SelfLink_Throws() =>
        Should.Throw<ArgumentException>(() => PartyRelationship.Create(Persona, Persona, "EMPLEADO"));

    [Fact]
    public void Create_EndBeforeStart_Throws()
    {
        var start = new DateOnly(2026, 6, 1);
        var end = new DateOnly(2026, 5, 1);
        Should.Throw<ArgumentException>(() =>
            PartyRelationship.Create(Persona, EmpresaA, "EMPLEADO", startDate: start, endDate: end));
    }

    [Fact]
    public void Create_RequiresRelationshipType() =>
        Should.Throw<ArgumentException>(() => PartyRelationship.Create(Persona, EmpresaA, " "));

    [Fact]
    public void SamePerson_TwoCompanies_AreTwoRelationships_OnePerson()
    {
        var r1 = PartyRelationship.Create(Persona, EmpresaA, "EMPLEADO");
        var r2 = PartyRelationship.Create(Persona, EmpresaB, "CONTACTO_EXTERNO");

        r1.Id.ShouldNotBe(r2.Id);
        r1.SourcePartyId.ShouldBe(Persona);
        r2.SourcePartyId.ShouldBe(Persona);   // la misma persona, no duplicada
        r1.TargetPartyId.ShouldNotBe(r2.TargetPartyId);
    }

    [Fact]
    public void Create_DefaultsStartToToday_AndActive()
    {
        var r = PartyRelationship.Create(Persona, EmpresaA, "SOCIO");
        r.IsActive.ShouldBeTrue();
        r.StartDate.ShouldBe(DateOnly.FromDateTime(DateTime.UtcNow));
    }

    [Fact]
    public void Deactivate_ClearsActiveAndPrimary()
    {
        var r = PartyRelationship.Create(Persona, EmpresaA, "EMPLEADO", isPrimary: true);
        r.IsPrimary.ShouldBeTrue();

        r.Deactivate(new DateOnly(2026, 6, 30));

        r.IsActive.ShouldBeFalse();
        r.IsPrimary.ShouldBeFalse();           // un vínculo inactivo no puede ser el principal
        r.EndDate.ShouldBe(new DateOnly(2026, 6, 30));
    }

    [Fact]
    public void Update_EndBeforeStart_Throws()
    {
        var r = PartyRelationship.Create(Persona, EmpresaA, "EMPLEADO");
        Should.Throw<ArgumentException>(() =>
            r.Update("EMPLEADO", null, null, new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 1)));
    }

    [Fact]
    public void SetCustomFields_RoundTrips_AndClears()
    {
        var r = PartyRelationship.Create(Persona, EmpresaA, "EMPLEADO");
        r.CustomFields.ShouldBeNull();

        r.SetCustomFields(JsonDocument.Parse("""{ "antiguedad": 3 }"""));
        r.CustomFields!.RootElement.GetProperty("antiguedad").GetInt32().ShouldBe(3);

        r.SetCustomFields(null);
        r.CustomFields.ShouldBeNull();
    }
}
