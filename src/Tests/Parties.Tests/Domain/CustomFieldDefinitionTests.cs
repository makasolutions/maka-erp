using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.CustomFields;

namespace Parties.Tests.Domain;

/// <summary>
/// PR-1: definición de custom field (esquema). Slug inmutable/normalizado, opciones obligatorias en
/// Select/MultiSelect, sin valores de opción duplicados.
/// </summary>
public class CustomFieldDefinitionTests
{
    private static CustomFieldDefinition NewText(string title, string? slug = null) =>
        CustomFieldDefinition.Create(CustomFieldEntityType.Party, title, slug, CustomFieldType.Text,
            null, isRequired: false, isUnique: false, isDefaultValueEnabled: false, null, isMultiselect: false, null);

    [Fact]
    public void Create_DerivesNormalizedSlug_FromTitle()
    {
        var def = NewText("Código de Cliente VIP");
        def.ApiSlug.ShouldBe("codigo_de_cliente_vip");
    }

    [Theory]
    [InlineData("  Nivel  de   Riesgo ", "nivel_de_riesgo")]
    [InlineData("Año-Fiscal!!", "ano_fiscal")]
    [InlineData("email@dominio", "email_dominio")]
    public void NormalizeSlug_StripsAccentsAndSymbols(string raw, string expected)
    {
        CustomFieldDefinition.NormalizeSlug(raw).ShouldBe(expected);
    }

    [Fact]
    public void Create_Select_WithoutOptions_Throws()
    {
        Should.Throw<ArgumentException>(() => CustomFieldDefinition.Create(
            CustomFieldEntityType.Party, "Segmento", null, CustomFieldType.Select,
            null, false, false, false, null, false, options: null));
    }

    [Fact]
    public void Create_Select_WithOptions_Ok_And_MarksMultiselectForMultiSelect()
    {
        var opts = new List<CustomFieldOption> { new("a", "Alfa", null), new("b", "Beta", "#fff") };
        var sel = CustomFieldDefinition.Create(CustomFieldEntityType.Party, "Segmento", null, CustomFieldType.Select,
            null, false, false, false, null, false, opts);
        sel.Options.Count.ShouldBe(2);
        sel.IsMultiselect.ShouldBeFalse();

        var multi = CustomFieldDefinition.Create(CustomFieldEntityType.Party, "Tags", null, CustomFieldType.MultiSelect,
            null, false, false, false, null, false, opts);
        multi.IsMultiselect.ShouldBeTrue();
    }

    [Fact]
    public void Create_Select_DuplicateOptionValues_Throws()
    {
        var opts = new List<CustomFieldOption> { new("a", "Alfa", null), new("a", "Otra", null) };
        Should.Throw<ArgumentException>(() => CustomFieldDefinition.Create(
            CustomFieldEntityType.Party, "Segmento", null, CustomFieldType.Select,
            null, false, false, false, null, false, opts));
    }

    [Fact]
    public void Deactivate_SetsActivoFalse()
    {
        var def = NewText("Notas internas");
        def.Deactivate();
        def.Activo.ShouldBeFalse();
    }
}
