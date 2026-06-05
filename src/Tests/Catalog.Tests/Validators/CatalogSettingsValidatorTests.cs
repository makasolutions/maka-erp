using FSH.Modules.Catalog.Contracts.v1.ShippingClasses.CreateShippingClass;
using FSH.Modules.Catalog.Contracts.v1.TaxRates.CreateTaxRate;
using FSH.Modules.Catalog.Features.v1.ShippingClasses.CreateShippingClass;
using FSH.Modules.Catalog.Features.v1.TaxRates.CreateTaxRate;

namespace Catalog.Tests.Validators;

public sealed class CatalogSettingsValidatorTests
{
    [Theory]
    [InlineData(0.19, true)]
    [InlineData(0.0, true)]
    [InlineData(1.0, true)]
    [InlineData(1.5, false)]
    [InlineData(-0.1, false)]
    public void CreateTaxRate_ValidatesRateRange(double rate, bool expected)
    {
        var r = new CreateTaxRateCommandValidator().Validate(
            new CreateTaxRateCommand("IVA", (decimal)rate));
        r.IsValid.ShouldBe(expected);
    }

    [Fact]
    public void CreateTaxRate_RejectsEmptyName()
    {
        new CreateTaxRateCommandValidator()
            .Validate(new CreateTaxRateCommand("", 0.19m))
            .IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateShippingClass_RejectsEmptyName()
    {
        new CreateShippingClassCommandValidator()
            .Validate(new CreateShippingClassCommand(""))
            .IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateShippingClass_AcceptsValid()
    {
        new CreateShippingClassCommandValidator()
            .Validate(new CreateShippingClassCommand("Frágil", "Manejo cuidadoso"))
            .IsValid.ShouldBeTrue();
    }
}
