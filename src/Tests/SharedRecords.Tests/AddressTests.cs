using FSH.Modules.SharedRecords.Domain;
using Shouldly;
using Xunit;

namespace SharedRecords.Tests;

public class AddressTests
{
    private static Address NewAddress(string ownerType, Guid ownerId, bool primary = false) =>
        Address.Create(ownerType, ownerId, labelCode: "BODEGA", isActive: true, isPrimary: primary,
            country: "Colombia", department: "Cundinamarca", city: "Bogotá", departmentCode: "11",
            municipalityCode: "11001", line: "CL 100 # 13-21", barrio: null, reference: null,
            latitude: null, longitude: null);

    [Fact]
    public void Create_SetsOwnerAndFields()
    {
        var ownerId = Guid.CreateVersion7();
        var a = NewAddress("Party", ownerId, primary: true);

        a.Id.ShouldNotBe(Guid.Empty);
        a.OwnerType.ShouldBe("Party");
        a.OwnerId.ShouldBe(ownerId);
        a.LabelCode.ShouldBe("BODEGA");
        a.IsActive.ShouldBeTrue();
        a.IsPrimary.ShouldBeTrue();
        a.MunicipalityCode.ShouldBe("11001");
        a.Line.ShouldBe("CL 100 # 13-21");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankOwnerType_Throws(string ownerType) =>
        Should.Throw<ArgumentException>(() => NewAddress(ownerType, Guid.CreateVersion7()));

    [Fact]
    public void Create_EmptyOwnerId_Throws() =>
        Should.Throw<ArgumentException>(() => NewAddress("Party", Guid.Empty));

    [Fact]
    public void EnsureSinglePrimary_PromotesTargetAndDemotesOthers()
    {
        var owner = Guid.CreateVersion7();
        var a = NewAddress("Party", owner, primary: true);
        var b = NewAddress("Party", owner, primary: true);   // estado inválido transitorio
        var c = NewAddress("Party", owner, primary: false);

        AddressPrimaryPolicy.EnsureSinglePrimary([a, b, c], b.Id);

        a.IsPrimary.ShouldBeFalse();
        b.IsPrimary.ShouldBeTrue();
        c.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void EnsureSinglePrimary_IsIdempotent()
    {
        var owner = Guid.CreateVersion7();
        var a = NewAddress("Party", owner, primary: true);
        var b = NewAddress("Party", owner);

        AddressPrimaryPolicy.EnsureSinglePrimary([a, b], a.Id);
        AddressPrimaryPolicy.EnsureSinglePrimary([a, b], a.Id); // re-ejecutar no cambia nada

        a.IsPrimary.ShouldBeTrue();
        b.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void EnsureSinglePrimary_TargetNotInList_DemotesAll()
    {
        var owner = Guid.CreateVersion7();
        var a = NewAddress("Party", owner, primary: true);
        var b = NewAddress("Party", owner);

        AddressPrimaryPolicy.EnsureSinglePrimary([a, b], Guid.CreateVersion7());

        a.IsPrimary.ShouldBeFalse();
        b.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void Update_OverwritesEditableFields()
    {
        var a = NewAddress("Party", Guid.CreateVersion7(), primary: true);
        a.Update(labelCode: "SEDE", isActive: false, isPrimary: false, country: "Colombia",
            department: "Antioquia", city: "Medellín", departmentCode: "05", municipalityCode: "05001",
            line: "CR 43A # 1-50", barrio: "El Poblado", reference: "Torre 1", latitude: 6.2m, longitude: -75.5m);

        a.LabelCode.ShouldBe("SEDE");
        a.IsActive.ShouldBeFalse();
        a.IsPrimary.ShouldBeFalse();
        a.City.ShouldBe("Medellín");
        a.MunicipalityCode.ShouldBe("05001");
        a.Latitude.ShouldBe(6.2m);
    }
}
