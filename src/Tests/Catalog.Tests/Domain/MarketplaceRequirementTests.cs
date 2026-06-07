using FSH.Modules.Catalog.Contracts.Enums;
using FSH.Modules.Catalog.Contracts.v1.Marketplaces;
using FSH.Modules.Catalog.Domain;
using FSH.Modules.Catalog.Features.v1.Marketplaces.SetCategoryRequirements;

namespace Catalog.Tests.Domain;

public sealed class MarketplaceRequirementTests
{
    [Fact]
    public void Create_Should_SetFields()
    {
        var categoryId = Guid.NewGuid();
        var attributeId = Guid.NewGuid();

        var req = MarketplaceAttributeRequirement.Create(Marketplace.MercadoLibre, categoryId, attributeId);

        req.Id.ShouldNotBe(Guid.Empty);
        req.Marketplace.ShouldBe(Marketplace.MercadoLibre);
        req.CategoryId.ShouldBe(categoryId);
        req.AttributeId.ShouldBe(attributeId);
    }
}

public sealed class SetCategoryRequirementsValidatorTests
{
    private readonly SetCategoryRequirementsCommandValidator _validator = new();

    [Fact]
    public void Should_RejectEmptyCategory()
    {
        var r = _validator.Validate(new SetCategoryRequirementsCommand(Guid.Empty, Marketplace.Google, [Guid.NewGuid()]));
        r.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Should_AcceptEmptyAttributeList()
    {
        // Empty list clears the requirements — valid.
        var r = _validator.Validate(new SetCategoryRequirementsCommand(Guid.NewGuid(), Marketplace.Google, []));
        r.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Should_RejectEmptyAttributeId()
    {
        var r = _validator.Validate(new SetCategoryRequirementsCommand(Guid.NewGuid(), Marketplace.Google, [Guid.Empty]));
        r.IsValid.ShouldBeFalse();
    }
}
