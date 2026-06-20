using System.Text.Json;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.CustomFields;

namespace Parties.Tests.Domain;

/// <summary>
/// PR-1: validación/coerción de VALORES de custom fields. Cubre los ajustes aprobados:
/// A (IsRequired gobernado: no bloquea captura mínima) y C (coerción por tipo).
/// </summary>
public class CustomFieldValuesTests
{
    private static CustomFieldDefinition Def(CustomFieldType type, bool required = false,
        IReadOnlyList<CustomFieldOption>? options = null) =>
        CustomFieldDefinition.Create(CustomFieldEntityType.Party, $"Campo {type}", apiSlug: null,
            type, null, required, false, false, null, false, options);

    private static JsonElement V(string json) => JsonDocument.Parse(json).RootElement;

    private static Dictionary<string, JsonElement> Bag(string jsonObject) =>
        JsonDocument.Parse(jsonObject).RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

    // ── Ajuste A: IsRequired gobernado ──────────────────────────────────────────────

    [Fact]
    public void RequiredMissing_Minimal_IsOk()
    {
        var def = CustomFieldDefinition.Create(CustomFieldEntityType.Party, "NIT Externo", "nit_externo",
            CustomFieldType.Text, null, isRequired: true, false, false, null, false, null);

        var errors = CustomFieldValues.ValidateAll([def], new Dictionary<string, JsonElement>(),
            CustomFieldCompletenessMode.Minimal);

        errors.ShouldBeEmpty();
    }

    [Fact]
    public void RequiredMissing_Complete_Fails()
    {
        var def = CustomFieldDefinition.Create(CustomFieldEntityType.Party, "NIT Externo", "nit_externo",
            CustomFieldType.Text, null, isRequired: true, false, false, null, false, null);

        var errors = CustomFieldValues.ValidateAll([def], new Dictionary<string, JsonElement>(),
            CustomFieldCompletenessMode.Complete);

        errors.Count.ShouldBe(1);
    }

    [Fact]
    public void RequiredPresent_Complete_IsOk()
    {
        var def = CustomFieldDefinition.Create(CustomFieldEntityType.Party, "NIT Externo", "nit_externo",
            CustomFieldType.Text, null, isRequired: true, false, false, null, false, null);

        var errors = CustomFieldValues.ValidateAll([def], Bag("""{ "nit_externo": "900123" }"""),
            CustomFieldCompletenessMode.Complete);

        errors.ShouldBeEmpty();
    }

    // ── Ajuste C: coerción por tipo ─────────────────────────────────────────────────

    [Fact]
    public void Number_AsText_Fails()
    {
        CustomFieldValues.ValidateValue(Def(CustomFieldType.Number), V("\"NaN\"")).ShouldNotBeNull();
        CustomFieldValues.ValidateValue(Def(CustomFieldType.Number), V("42")).ShouldBeNull();
    }

    [Fact]
    public void Checkbox_RequiresBoolean()
    {
        CustomFieldValues.ValidateValue(Def(CustomFieldType.Checkbox), V("true")).ShouldBeNull();
        CustomFieldValues.ValidateValue(Def(CustomFieldType.Checkbox), V("\"yes\"")).ShouldNotBeNull();
    }

    [Fact]
    public void Date_InvalidString_Fails()
    {
        CustomFieldValues.ValidateValue(Def(CustomFieldType.Date), V("\"2026-13-40\"")).ShouldNotBeNull();
        CustomFieldValues.ValidateValue(Def(CustomFieldType.Date), V("\"2026-06-19\"")).ShouldBeNull();
    }

    [Fact]
    public void Select_OutsideOptions_Fails()
    {
        var def = Def(CustomFieldType.Select, options: new List<CustomFieldOption> { new("vip", "VIP", null), new("std", "Estándar", null) });
        CustomFieldValues.ValidateValue(def, V("\"vip\"")).ShouldBeNull();
        CustomFieldValues.ValidateValue(def, V("\"otro\"")).ShouldNotBeNull();
    }

    [Fact]
    public void MultiSelect_AllWithinOptions_Ok()
    {
        var def = Def(CustomFieldType.MultiSelect, options: new List<CustomFieldOption> { new("a", "A", null), new("b", "B", null) });
        CustomFieldValues.ValidateValue(def, V("""["a","b"]""")).ShouldBeNull();
        CustomFieldValues.ValidateValue(def, V("""["a","z"]""")).ShouldNotBeNull();
    }

    [Fact]
    public void Email_BasicShape_Validated()
    {
        CustomFieldValues.ValidateValue(Def(CustomFieldType.EmailAddress), V("\"a@b.com\"")).ShouldBeNull();
        CustomFieldValues.ValidateValue(Def(CustomFieldType.EmailAddress), V("\"no-arroba\"")).ShouldNotBeNull();
    }
}
