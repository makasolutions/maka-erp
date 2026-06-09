using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// "Traductor" de direcciones: toma lo que escribe el cliente (con #, No, guiones, nombres
/// completos de vía) y produce la forma <b>codificada DIAN</b> — abreviatura canónica de la vía,
/// separada por espacios, en mayúsculas y sin diacríticos. La línea cruda nunca se pierde; la
/// normalizada es derivada y recomputable. Pragmático: cubre las vías y complementos comunes;
/// se refina iterando. Ej.: "Carrera 53C No 131A - 91" → "KR 53C 131A 91".
/// </summary>
public static partial class DianAddressNormalizer
{
    // Vías de una palabra → abreviatura canónica DIAN.
    private static readonly Dictionary<string, string> Via = new(StringComparer.Ordinal)
    {
        ["CALLE"] = "CL", ["CL"] = "CL", ["CLL"] = "CL",
        ["CARRERA"] = "KR", ["CRA"] = "KR", ["CR"] = "KR", ["KR"] = "KR", ["KRA"] = "KR",
        ["AVENIDA"] = "AV", ["AV"] = "AV", ["AVE"] = "AV",
        ["DIAGONAL"] = "DG", ["DIAG"] = "DG", ["DG"] = "DG",
        ["TRANSVERSAL"] = "TV", ["TRANSV"] = "TV", ["TRV"] = "TV", ["TV"] = "TV",
        ["CIRCULAR"] = "CQ", ["CQ"] = "CQ",
        ["CIRCUNVALAR"] = "CV", ["CV"] = "CV",
        ["AUTOPISTA"] = "AU", ["AUT"] = "AU", ["AU"] = "AU",
        ["KILOMETRO"] = "KM", ["KM"] = "KM",
        ["MANZANA"] = "MZ", ["MZ"] = "MZ", ["MZN"] = "MZ",
        ["VEREDA"] = "VRD", ["VRD"] = "VRD", ["VDA"] = "VRD",
        ["AVCALLE"] = "AC", ["AC"] = "AC",
        ["AVCARRERA"] = "AK", ["AK"] = "AK",
    };

    // Complementos comunes → abreviatura DIAN (se mapean token a token donde aparezcan).
    private static readonly Dictionary<string, string> Complement = new(StringComparer.Ordinal)
    {
        ["APARTAMENTO"] = "AP", ["APTO"] = "AP", ["APT"] = "AP", ["APARTAMENTOS"] = "AP",
        ["INTERIOR"] = "IN", ["INT"] = "IN",
        ["BLOQUE"] = "BL", ["BLQ"] = "BL",
        ["TORRE"] = "TO", ["TOR"] = "TO",
        ["EDIFICIO"] = "ED", ["EDIF"] = "ED",
        ["OFICINA"] = "OF", ["OFC"] = "OF",
        ["LOCAL"] = "LC",
        ["BODEGA"] = "BG", ["BOD"] = "BG",
        ["PISO"] = "PI",
        ["CASA"] = "CA",
    };

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex WhitespaceRegex();

    // Separadores de numeración a colapsar: # - y los tokens No/Nro/N°/Número.
    [GeneratedRegex(@"[#\-]|\bN(?:O|RO|UMERO)?\.?\b|\bN[°º]", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SeparatorRegex();

    /// <summary>Devuelve la forma codificada DIAN, o null si la entrada está vacía.</summary>
    public static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        string s = RemoveDiacritics(raw.Trim().ToUpperInvariant());
        s = SeparatorRegex().Replace(s, " ");
        s = WhitespaceRegex().Replace(s, " ").Trim();
        if (s.Length == 0) return null;

        var tokens = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var outTokens = new List<string>(tokens.Length);

        int start = 0;
        // Vía de dos palabras (AVENIDA CALLE / AVENIDA CARRERA) primero.
        if (tokens.Length >= 2 && Via.TryGetValue(tokens[0] + tokens[1], out var two))
        {
            outTokens.Add(two);
            start = 2;
        }
        else if (Via.TryGetValue(tokens[0], out var one))
        {
            outTokens.Add(one);
            start = 1;
        }

        for (int i = start; i < tokens.Length; i++)
            outTokens.Add(Complement.TryGetValue(tokens[i], out var c) ? c : tokens[i]);

        return string.Join(' ', outTokens);
    }

    private static string RemoveDiacritics(string text)
    {
        string normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (char ch in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                sb.Append(ch);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
