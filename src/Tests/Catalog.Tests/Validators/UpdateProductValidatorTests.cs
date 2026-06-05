using FSH.Modules.Catalog.Contracts.v1.Products.UpdateProduct;
using FSH.Modules.Catalog.Features.v1.Products.UpdateProduct;

namespace Catalog.Tests.Validators;

public sealed class UpdateProductValidatorTests
{
    private static UpdateProductCommand Base(string? specs = null) => new(
        Id: Guid.NewGuid(),
        Name: "Sony FX3",
        ShortDescription: null,
        Description: null,
        TechnicalSpecs: null,
        BrandId: null,
        TaxRateId: null,
        ShippingClassId: null,
        IsVirtual: false,
        IsDownloadable: false,
        IsPublic: false,
        Weight: null,
        WeightUnit: "KG",
        DimensionLength: null,
        DimensionWidth: null,
        DimensionHeight: null,
        DimensionUnit: "CM",
        SeoTitle: null,
        SeoDescription: null,
        SeoKeywords: null,
        Specs: specs);

    [Fact]
    public void Accepts_NullSpecs()
    {
        new UpdateProductCommandValidator().Validate(Base(null)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Accepts_ValidJsonSpecs()
    {
        var r = new UpdateProductCommandValidator().Validate(
            Base("{\"sensor\":\"12MP\",\"video\":\"4K120fps\"}"));
        r.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Rejects_InvalidJsonSpecs()
    {
        var r = new UpdateProductCommandValidator().Validate(Base("{not valid json"));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Rejects_SeoTitleOver60()
    {
        var r = new UpdateProductCommandValidator().Validate(
            Base() with { SeoTitle = new string('a', 61) });
        r.IsValid.ShouldBeFalse();
    }
}
