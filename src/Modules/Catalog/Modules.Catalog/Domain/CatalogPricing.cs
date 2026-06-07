namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Reglas de cálculo de precios derivados (Fase 3).
/// Las listas no-default derivan su precio de la lista por defecto aplicando un
/// porcentaje (+ incremento / − descuento) y redondeando al $10.000 más cercano
/// menos $1.000 (valor sugerido y editable por el usuario).
/// </summary>
public static class CatalogPricing
{
    /// <summary>Redondea al $10.000 más cercano y resta $1.000. Nunca negativo.</summary>
    public static decimal RoundSuggested(decimal value)
    {
        if (value <= 0) return 0m;
        decimal rounded = Math.Round(value / 10000m, MidpointRounding.AwayFromZero) * 10000m;
        decimal result = rounded - 1000m;
        return result < 0m ? 0m : result;
    }

    /// <summary>Precio derivado = base × (1 + %/100), redondeado.</summary>
    public static decimal Derive(decimal basePrice, decimal adjustmentPercent)
        => RoundSuggested(basePrice * (1m + (adjustmentPercent / 100m)));
}
