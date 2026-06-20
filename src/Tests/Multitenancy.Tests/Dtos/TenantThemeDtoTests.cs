using FSH.Modules.Multitenancy.Contracts.Dtos;

namespace Multitenancy.Tests.Dtos;

/// <summary>
/// Regresión del bug de theming Dark: el fallback <see cref="TenantThemeDto.Default"/> (devuelto cuando un
/// tenant no tiene tema propio) debe marcar <c>IsDefault=true</c> (para que el front NO aplique branding
/// inline y deje gobernar el CSS <c>:root</c>/<c>.dark</c>) y exponer la paleta dark REAL. Antes daba
/// <c>IsDefault=false</c> + <c>DarkPalette=new()</c> (= paleta light) → dark mode quedaba claro para root.
/// </summary>
public sealed class TenantThemeDtoTests
{
    [Fact]
    public void Default_IsMarkedAsDefault()
    {
        TenantThemeDto.Default.IsDefault.ShouldBeTrue();
    }

    [Fact]
    public void Default_UsesRealDarkPalette_NotLight()
    {
        var dark = TenantThemeDto.Default.DarkPalette;

        dark.ShouldBe(PaletteDto.DefaultDark);
        // Sanidad: la dark NO debe ser la light (surface/background claros era el bug).
        dark.Surface.ShouldBe("#111827");
        dark.Background.ShouldBe("#0B1220");
        dark.Surface.ShouldNotBe(PaletteDto.DefaultLight.Surface);
        dark.Background.ShouldNotBe(PaletteDto.DefaultLight.Background);
    }

    [Fact]
    public void Default_UsesLightPalette_ForLight()
    {
        TenantThemeDto.Default.LightPalette.ShouldBe(PaletteDto.DefaultLight);
    }
}
