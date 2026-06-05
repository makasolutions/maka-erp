using System.Collections.Generic;
using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Attributes.AddAttributeValue;
using FSH.Modules.Catalog.Contracts.v1.Attributes.CreateAttribute;
using FSH.Modules.Catalog.Contracts.v1.Products.SetProductAttributes;
using FSH.Modules.Catalog.Features.v1.Attributes.AddAttributeValue;
using FSH.Modules.Catalog.Features.v1.Attributes.CreateAttribute;
using FSH.Modules.Catalog.Features.v1.Products.SetProductAttributes;

namespace Catalog.Tests.Validators;

public sealed class AttributeValidatorTests
{
    [Fact]
    public void CreateAttribute_Should_RejectEmptyName()
    {
        var r = new CreateAttributeCommandValidator().Validate(
            new CreateAttributeCommand("", null, CatalogAttributeType.Select));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateAttribute_Should_RejectBadSlug()
    {
        var r = new CreateAttributeCommandValidator().Validate(
            new CreateAttributeCommand("Color", "Bad Slug!", CatalogAttributeType.Color));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateAttribute_Should_AcceptValid()
    {
        var r = new CreateAttributeCommandValidator().Validate(
            new CreateAttributeCommand("Color", "color", CatalogAttributeType.Color, IsUsedForVariations: true));
        r.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("#FF0000", true)]
    [InlineData("#FF0000AA", true)]
    [InlineData("red", false)]
    [InlineData("FF0000", false)]
    public void AddAttributeValue_Should_ValidateColorCode(string color, bool expected)
    {
        var r = new AddAttributeValueCommandValidator().Validate(
            new AddAttributeValueCommand(Guid.NewGuid(), "Rojo", ColorCode: color));
        r.IsValid.ShouldBe(expected);
    }

    [Fact]
    public void AddAttributeValue_Should_RejectEmptyValue()
    {
        var r = new AddAttributeValueCommandValidator().Validate(
            new AddAttributeValueCommand(Guid.NewGuid(), ""));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetProductAttributes_Should_RequireValues_WhenUsedForVariations()
    {
        var assignments = new List<ProductAttributeAssignment>
        {
            new(Guid.NewGuid(), [], IsUsedForVariations: true),
        };
        var r = new SetProductAttributesCommandValidator().Validate(
            new SetProductAttributesCommand(Guid.NewGuid(), assignments));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetProductAttributes_Should_Accept_NonVariationAttributeWithoutValues()
    {
        var assignments = new List<ProductAttributeAssignment>
        {
            new(Guid.NewGuid(), [], IsUsedForVariations: false),
        };
        var r = new SetProductAttributesCommandValidator().Validate(
            new SetProductAttributesCommand(Guid.NewGuid(), assignments));
        r.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void SetProductAttributes_Should_Accept_VariationAttributeWithValues()
    {
        var assignments = new List<ProductAttributeAssignment>
        {
            new(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()], IsUsedForVariations: true),
        };
        var r = new SetProductAttributesCommandValidator().Validate(
            new SetProductAttributesCommand(Guid.NewGuid(), assignments));
        r.IsValid.ShouldBeTrue();
    }
}
