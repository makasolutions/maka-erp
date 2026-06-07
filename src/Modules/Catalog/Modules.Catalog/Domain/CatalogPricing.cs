namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Reglas de cálculo de precios derivados (Fase 3 + cierre).
/// Las listas no-default derivan su precio del valor <b>IVA incluido</b> de la lista
/// por defecto: se aplica el porcentaje (+ incremento / − descuento) sobre el IVA
/// incluido y, si la lista tiene <c>RoundEnabled</c>, se redondea ese valor al
/// $10.000 más cercano menos $1.000. La base (sin IVA) se deduce dividiendo por
/// (1 + IVA). El valor es sugerido y editable por el usuario.
/// </summary>
public static class CatalogPricing
{
    /// <summary>IVA general Colombia (19%).</summary>
    public const decimal IvaRate = 0.19m;

    /// <summary>Redondea al $10.000 más cercano y resta $1.000. Nunca negativo.</summary>
    public static decimal RoundSuggested(decimal value)
    {
        if (value <= 0) return 0m;
        decimal rounded = Math.Round(value / 10000m, MidpointRounding.AwayFromZero) * 10000m;
        decimal result = rounded - 1000m;
        return result < 0m ? 0m : result;
    }

    public static decimal ToTaxIncluded(decimal basePrice) => basePrice * (1m + IvaRate);
    public static decimal FromTaxIncluded(decimal taxIncluded) => taxIncluded / (1m + IvaRate);

    /// <summary>
    /// Base derivada de la lista por defecto. Se calcula sobre el IVA incluido:
    /// ivaInclDerivado = ivaInclDefault × (1 + %/100); si <paramref name="round"/>,
    /// se redondea; la base resultante = ivaInclDerivado / (1 + IVA).
    /// </summary>
    public static decimal DeriveBase(decimal baseDefault, decimal adjustmentPercent, bool round)
    {
        decimal ivaInclDefault = ToTaxIncluded(baseDefault);
        decimal ivaInclDerived = ivaInclDefault * (1m + (adjustmentPercent / 100m));
        if (round) ivaInclDerived = RoundSuggested(ivaInclDerived);
        return FromTaxIncluded(ivaInclDerived);
    }
}
