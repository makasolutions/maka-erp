using FSH.Modules.Catalog.Domain;

namespace Catalog.Tests.Domain;

public sealed class VariationAttributeTests
{
    [Fact]
    public void SetAttributeValues_Should_ReplaceCombination()
    {
        var variation = ProductVariation.Create(Guid.NewGuid(), "FX3-ROJO-XL");
        var rojo = CatalogAttributeValue.Create(Guid.NewGuid(), "Rojo");
        var xl   = CatalogAttributeValue.Create(Guid.NewGuid(), "XL");

        variation.SetAttributeValues([rojo, xl]);
        variation.AttributeValues.Count.ShouldBe(2);

        // Replacing clears the prior combination.
        variation.SetAttributeValues([rojo]);
        variation.AttributeValues.Count.ShouldBe(1);
        variation.AttributeValues.ShouldContain(rojo);
    }

    [Fact]
    public void SetAttributeValues_Should_StampUpdatedAt()
    {
        var variation = ProductVariation.Create(Guid.NewGuid(), "SKU-1");
        variation.UpdatedAtUtc.ShouldBeNull();
        variation.SetAttributeValues([CatalogAttributeValue.Create(Guid.NewGuid(), "Rojo")]);
        variation.UpdatedAtUtc.ShouldNotBeNull();
    }
}
