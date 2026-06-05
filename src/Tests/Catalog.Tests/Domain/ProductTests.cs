using System.Linq;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class ProductTests
{
    [Theory]
    [InlineData(ProductType.Simple)]
    [InlineData(ProductType.Service)]
    public void Create_Should_GenerateDefaultVariation_When_SimpleOrService(ProductType type)
    {
        var p = Product.Create("Sony FX3", "sony-fx3", type);

        p.Variations.Count.ShouldBe(1);
        var v = p.Variations.Single();
        v.IsDefault.ShouldBeTrue();
        v.Sku.ShouldBe("SONY-FX3-DEFAULT");
    }

    [Theory]
    [InlineData(ProductType.Variable)]
    [InlineData(ProductType.Bundle)]
    public void Create_Should_NotGenerateDefaultVariation_When_VariableOrBundle(ProductType type)
    {
        var p = Product.Create("Kit", "kit", type);
        p.Variations.ShouldBeEmpty();
    }

    [Fact]
    public void Create_Should_UseProvidedDefaultSku_When_Given()
    {
        var p = Product.Create("Sony FX3", "sony-fx3", ProductType.Simple, defaultSku: "FX3-001");
        p.Variations.Single().Sku.ShouldBe("FX3-001");
    }

    [Fact]
    public void Create_Should_StartAsDraft()
    {
        var p = Product.Create("X", "x", ProductType.Simple);
        p.Status.ShouldBe(ProductStatus.Draft);
    }

    [Fact]
    public void Publish_Should_SetStatusActive()
    {
        var p = Product.Create("X", "x", ProductType.Simple);
        p.Publish();
        p.Status.ShouldBe(ProductStatus.Active);
    }

    [Fact]
    public void Archive_Should_SetStatusArchived()
    {
        var p = Product.Create("X", "x", ProductType.Simple);
        p.Publish();
        p.Archive();
        p.Status.ShouldBe(ProductStatus.Archived);
    }

    [Fact]
    public void Create_Should_ThrowOnBlankName()
    {
        Should.Throw<System.ArgumentException>(() => Product.Create("  ", "x", ProductType.Simple));
    }
}
