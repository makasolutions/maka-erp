using System.Collections.Generic;
using FSH.Modules.Catalog.Contracts.v1.ProductImages.AddProductImage;
using FSH.Modules.Catalog.Contracts.v1.ProductTags.SetProductTags;
using FSH.Modules.Catalog.Features.v1.ProductImages.AddProductImage;
using FSH.Modules.Catalog.Features.v1.ProductTags.SetProductTags;

namespace Catalog.Tests.Validators;

public sealed class MediaValidatorTests
{
    [Fact]
    public void AddProductImage_Should_RejectRelativeUrl()
    {
        var r = new AddProductImageCommandValidator().Validate(
            new AddProductImageCommand(Guid.NewGuid(), "/images/x.jpg"));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void AddProductImage_Should_AcceptAbsoluteUrl()
    {
        var r = new AddProductImageCommandValidator().Validate(
            new AddProductImageCommand(Guid.NewGuid(), "https://cdn.maka.co/x.jpg", AltText: "Frente"));
        r.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetProductTags_Should_RejectBadColor()
    {
        var tags = new List<ProductTagInput> { new("Oferta", "red") };
        var r = new SetProductTagsCommandValidator().Validate(
            new SetProductTagsCommand(Guid.NewGuid(), tags));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetProductTags_Should_RejectEmptyName()
    {
        var tags = new List<ProductTagInput> { new("", "#FF0000") };
        var r = new SetProductTagsCommandValidator().Validate(
            new SetProductTagsCommand(Guid.NewGuid(), tags));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetProductTags_Should_AcceptValid()
    {
        var tags = new List<ProductTagInput> { new("Oferta", "#FF0000"), new("Nuevo") };
        var r = new SetProductTagsCommandValidator().Validate(
            new SetProductTagsCommand(Guid.NewGuid(), tags));
        r.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetProductTags_Should_AcceptEmptyList()
    {
        var r = new SetProductTagsCommandValidator().Validate(
            new SetProductTagsCommand(Guid.NewGuid(), []));
        r.IsValid.ShouldBeTrue();
    }
}
