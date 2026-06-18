using FSH.Modules.SharedRecords.Domain;
using Shouldly;
using Xunit;

namespace SharedRecords.Tests;

public class PhoneTests
{
    private static Phone NewPhone(string ownerType, Guid ownerId, bool primary = false, string number = "3201234567") =>
        Phone.Create(ownerType, ownerId, typeCode: "CELULAR", isActive: true, isPrimary: primary,
            number: number, extension: null, countryCode: "+57");

    [Fact]
    public void Create_SetsOwnerAndFields()
    {
        var ownerId = Guid.CreateVersion7();
        var p = NewPhone("Party", ownerId, primary: true);

        p.Id.ShouldNotBe(Guid.Empty);
        p.OwnerType.ShouldBe("Party");
        p.OwnerId.ShouldBe(ownerId);
        p.TypeCode.ShouldBe("CELULAR");
        p.IsActive.ShouldBeTrue();
        p.IsPrimary.ShouldBeTrue();
        p.Number.ShouldBe("3201234567");
        p.CountryCode.ShouldBe("+57");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_BlankOwnerType_Throws(string ownerType) =>
        Should.Throw<ArgumentException>(() => NewPhone(ownerType, Guid.CreateVersion7()));

    [Fact]
    public void Create_EmptyOwnerId_Throws() =>
        Should.Throw<ArgumentException>(() => NewPhone("Party", Guid.Empty));

    [Fact]
    public void EnsureSinglePrimary_PromotesTargetAndDemotesOthers()
    {
        var owner = Guid.CreateVersion7();
        var a = NewPhone("Party", owner, primary: true);
        var b = NewPhone("Party", owner, primary: true);   // estado inválido transitorio
        var c = NewPhone("Party", owner, primary: false);

        PhonePrimaryPolicy.EnsureSinglePrimary([a, b, c], b.Id);

        a.IsPrimary.ShouldBeFalse();
        b.IsPrimary.ShouldBeTrue();
        c.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void EnsureSinglePrimary_IsIdempotent()
    {
        var owner = Guid.CreateVersion7();
        var a = NewPhone("Party", owner, primary: true);
        var b = NewPhone("Party", owner);

        PhonePrimaryPolicy.EnsureSinglePrimary([a, b], a.Id);
        PhonePrimaryPolicy.EnsureSinglePrimary([a, b], a.Id);

        a.IsPrimary.ShouldBeTrue();
        b.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void EnsureSinglePrimary_TargetNotInList_DemotesAll()
    {
        var owner = Guid.CreateVersion7();
        var a = NewPhone("Party", owner, primary: true);
        var b = NewPhone("Party", owner);

        PhonePrimaryPolicy.EnsureSinglePrimary([a, b], Guid.CreateVersion7());

        a.IsPrimary.ShouldBeFalse();
        b.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void Update_OverwritesEditableFields()
    {
        var p = NewPhone("Party", Guid.CreateVersion7(), primary: true);
        p.Update(typeCode: "WHATSAPP", isActive: false, isPrimary: false,
            number: "6012345678", extension: "101", countryCode: null);

        p.TypeCode.ShouldBe("WHATSAPP");
        p.IsActive.ShouldBeFalse();
        p.IsPrimary.ShouldBeFalse();
        p.Number.ShouldBe("6012345678");
        p.Extension.ShouldBe("101");
        p.CountryCode.ShouldBeNull();
    }
}
