using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class CatalogAttributeUpdateTests
{
    [Fact]
    public void Update_Should_ChangeFields_AndStampUpdatedAt()
    {
        var attr = CatalogAttribute.Create("Size", "size", CatalogAttributeType.Select);
        attr.UpdatedAtUtc.ShouldBeNull();

        attr.Update("Talla", CatalogAttributeType.Text, isVisibleOnProduct: false, isUsedForVariations: true, sortOrder: 5);

        attr.Name.ShouldBe("Talla");
        attr.Type.ShouldBe(CatalogAttributeType.Text);
        attr.IsVisibleOnProduct.ShouldBeFalse();
        attr.IsUsedForVariations.ShouldBeTrue();
        attr.SortOrder.ShouldBe(5);
        attr.UpdatedAtUtc.ShouldNotBeNull();
        // Slug is immutable on update.
        attr.Slug.ShouldBe("size");
    }
}

public sealed class CatalogAttributeValueUpdateTests
{
    [Fact]
    public void Update_Should_TrimValue_AndReplaceMetadata()
    {
        var v = CatalogAttributeValue.Create(Guid.NewGuid(), "Rojo", colorCode: "#FF0000");
        v.Update("  Negro  ", colorCode: "#000000", imageUrl: null, sortOrder: 3);

        v.Value.ShouldBe("Negro");
        v.ColorCode.ShouldBe("#000000");
        v.ImageUrl.ShouldBeNull();
        v.SortOrder.ShouldBe(3);
    }

    [Fact]
    public void Update_Should_Throw_OnBlankValue()
    {
        var v = CatalogAttributeValue.Create(Guid.NewGuid(), "Rojo");
        Should.Throw<ArgumentException>(() => v.Update("  ", null, null, 0));
    }
}

public sealed class CategoryAttributeTests
{
    [Fact]
    public void Create_Should_SetLinkFields()
    {
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var link = CategoryAttribute.Create(categoryId, attributeId, sortOrder: 2);

        link.Id.ShouldNotBe(Guid.Empty);
        link.CategoryId.ShouldBe(categoryId);
        link.AttributeId.ShouldBe(attributeId);
        link.SortOrder.ShouldBe(2);
        link.CreatedAtUtc.ShouldBeGreaterThan(DateTime.UtcNow.AddMinutes(-1));
    }
}

public sealed class ProductAttributeTests
{
    [Fact]
    public void SetSelectedValues_Should_ReplaceCollection()
    {
        var attributeId = Guid.NewGuid();
        var pa = ProductAttribute.Create(Guid.NewGuid(), attributeId, isUsedForVariations: true);
        var v1 = CatalogAttributeValue.Create(attributeId, "Rojo");
        var v2 = CatalogAttributeValue.Create(attributeId, "Negro");

        pa.SetSelectedValues([v1, v2]);
        pa.SelectedValues.Count.ShouldBe(2);

        // Replacing clears the previous set.
        pa.SetSelectedValues([v1]);
        pa.SelectedValues.Count.ShouldBe(1);
        pa.SelectedValues.ShouldContain(v1);
    }
}
