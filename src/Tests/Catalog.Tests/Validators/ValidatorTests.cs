using System.Collections.Generic;
using System.Linq;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Brands.CreateBrand;
using FSH.Modules.Catalog.Contracts.v1.PriceLists.CreatePriceList;
using FSH.Modules.Catalog.Contracts.v1.ProductCodes.AddProductCode;
using FSH.Modules.Catalog.Contracts.v1.Products.CreateProduct;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductCategories;
using FSH.Modules.Catalog.Features.v1.Brands.CreateBrand;
using FSH.Modules.Catalog.Features.v1.PriceLists.CreatePriceList;
using FSH.Modules.Catalog.Features.v1.ProductCodes.AddProductCode;
using FSH.Modules.Catalog.Features.v1.Products.CreateProduct;
using FSH.Modules.Catalog.Features.v1.Products.SetProductCategories;

namespace Catalog.Tests.Validators;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateBrand_Should_RejectEmptyName()
    {
        var r = new CreateBrandCommandValidator().Validate(new CreateBrandCommand("", null, null, null, null, null));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateBrand_Should_RejectBadCountryCode()
    {
        var r = new CreateBrandCommandValidator().Validate(
            new CreateBrandCommand("Sony", null, null, null, null, "Japan"));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateBrand_Should_AcceptValid()
    {
        var r = new CreateBrandCommandValidator().Validate(
            new CreateBrandCommand("Sony", "sony", null, null, null, "JP"));
        r.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CreateProduct_Should_RejectEmptyName()
    {
        var r = new CreateProductCommandValidator().Validate(
            new CreateProductCommand("", null, ProductType.Simple, null, null, null, null, null));
        r.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("retail", true)]
    [InlineData("wholesale", true)]
    [InlineData("invalid-segment", false)]
    public void CreatePriceList_Should_ValidateSegment(string segment, bool expected)
    {
        var r = new CreatePriceListCommandValidator().Validate(
            new CreatePriceListCommand("List", segment));
        r.IsValid.ShouldBe(expected);
    }

    [Fact]
    public void AddProductCode_Should_RequireSupplierId_When_SupplierCode()
    {
        var r = new AddProductCodeCommandValidator().Validate(
            new AddProductCodeCommand(Guid.NewGuid(), Guid.NewGuid(), "SupplierCode", "ABC", null, false));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AddProductCode_Should_AcceptEan_WithoutSupplier()
    {
        var r = new AddProductCodeCommandValidator().Validate(
            new AddProductCodeCommand(Guid.NewGuid(), Guid.NewGuid(), "EAN", "4548736130951", null, true));
        r.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetProductCategories_Should_RejectMultiplePrimary()
    {
        var cats = new List<ProductCategoryAssignment>
        {
            new(Guid.NewGuid(), true),
            new(Guid.NewGuid(), true),
        };
        var r = new SetProductCategoriesCommandValidator().Validate(
            new SetProductCategoriesCommand(Guid.NewGuid(), cats));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetProductCategories_Should_AcceptSinglePrimary()
    {
        var cats = new List<ProductCategoryAssignment>
        {
            new(Guid.NewGuid(), true),
            new(Guid.NewGuid(), false),
        };
        var r = new SetProductCategoriesCommandValidator().Validate(
            new SetProductCategoriesCommand(Guid.NewGuid(), cats));
        r.IsValid.ShouldBeTrue();
    }
}
