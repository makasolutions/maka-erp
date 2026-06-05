using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class ProductImageTests
{
    [Fact]
    public void Create_Should_TrimUrl_AndDefaultNotPrimary()
    {
        var img = ProductImage.Create(Guid.NewGuid(), "  https://cdn/x.jpg  ");
        img.Url.ShouldBe("https://cdn/x.jpg");
        img.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void SetPrimary_Should_ToggleFlag()
    {
        var img = ProductImage.Create(Guid.NewGuid(), "https://cdn/x.jpg");
        img.SetPrimary(true);
        img.IsPrimary.ShouldBeTrue();
        img.SetPrimary(false);
        img.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public void Create_Should_Throw_OnBlankUrl()
    {
        Should.Throw<ArgumentException>(() => ProductImage.Create(Guid.NewGuid(), "  "));
    }
}

public sealed class ProductTagTests
{
    [Fact]
    public void Create_Should_TrimName_AndKeepColor()
    {
        var tag = ProductTag.Create(Guid.NewGuid(), "  Oferta  ", "#FF0000");
        tag.Name.ShouldBe("Oferta");
        tag.Color.ShouldBe("#FF0000");
    }

    [Fact]
    public void Create_Should_Throw_OnBlankName()
    {
        Should.Throw<ArgumentException>(() => ProductTag.Create(Guid.NewGuid(), "  "));
    }
}
