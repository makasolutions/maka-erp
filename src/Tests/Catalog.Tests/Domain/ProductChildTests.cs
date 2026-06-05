using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class ProductCategoryTests
{
    [Fact]
    public void SetPrimary_Should_ToggleFlag()
    {
        var pc = ProductCategory.Create(Guid.NewGuid(), Guid.NewGuid(), isPrimary: false);
        pc.IsPrimary.ShouldBeFalse();
        pc.SetPrimary(true);
        pc.IsPrimary.ShouldBeTrue();
        pc.SetPrimary(false);
        pc.IsPrimary.ShouldBeFalse();
    }
}

public sealed class CatalogAttributeTests
{
    [Fact]
    public void Create_Should_LowercaseSlug_AndKeepFlags()
    {
        var attr = CatalogAttribute.Create("Color", "COLOR", CatalogAttributeType.Color, isUsedForVariations: true);
        attr.Name.ShouldBe("Color");
        attr.Slug.ShouldBe("color");
        attr.Type.ShouldBe(CatalogAttributeType.Color);
        attr.IsUsedForVariations.ShouldBeTrue();
    }

    [Fact]
    public void Value_Create_Should_TrimValue()
    {
        var v = CatalogAttributeValue.Create(Guid.NewGuid(), "  Rojo  ", colorCode: "#FF0000");
        v.Value.ShouldBe("Rojo");
        v.ColorCode.ShouldBe("#FF0000");
    }
}

public sealed class PriceBulkProposalTests
{
    private static PriceBulkProposal NewPending() => PriceBulkProposal.Create(
        batchId: Guid.NewGuid(),
        priceListId: Guid.NewGuid(),
        variationId: Guid.NewGuid(),
        supplierId: Guid.NewGuid(),
        supplierCode: "SUP-1",
        newPrice: 1000m,
        createdByUserId: "user-1");

    [Fact]
    public void Create_Should_StartPending()
    {
        NewPending().Status.ShouldBe(PriceProposalStatus.Pending);
    }

    [Fact]
    public void Approve_Should_SetApprovedAndDecider()
    {
        var p = NewPending();
        p.Approve("approver");
        p.Status.ShouldBe(PriceProposalStatus.Approved);
        p.DecidedByUserId.ShouldBe("approver");
        p.DecidedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Reject_Should_SetRejected()
    {
        var p = NewPending();
        p.Reject("approver");
        p.Status.ShouldBe(PriceProposalStatus.Rejected);
    }

    [Fact]
    public void Approve_Should_BeNoOp_When_AlreadyDecided()
    {
        var p = NewPending();
        p.Reject("a");
        p.Approve("b");
        p.Status.ShouldBe(PriceProposalStatus.Rejected);
    }
}
