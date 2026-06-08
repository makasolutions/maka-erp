using System.Text.RegularExpressions;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Reglas de validación reutilizables por tipo de dato (texto/persona, URL, teléfono,
/// dirección DIAN, geocoordenadas, fechas). Fuente de verdad del backend; el front
/// replica estas mismas reglas para feedback inmediato. Ver §"Estándar de validación
/// de formularios" en CLAUDE.md.
/// </summary>
public static partial class FormValidationRules
{
    public const int NameMaxLength = 50;
    public const int LegalNameMaxLength = 150;
    public const int UrlMaxLength = 2048;

    // ── Nombres de persona ────────────────────────────────────────────────
    // Letras Unicode (incl. tildes/ñ) + espacio y . ' - ; sin dígitos.
    [GeneratedRegex(@"^[\p{L}][\p{L}\p{M}\s.'\-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex PersonNameRegex();

    // Bloquea repetición de 8+ veces del mismo carácter (ej. "Ayyyyyyyy…").
    [GeneratedRegex(@"(.)\1{7,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExcessiveRepeatRegex();

    public static bool IsValidPersonName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true; // requerido se valida aparte
        var v = value.Trim();
        return PersonNameRegex().IsMatch(v) && !ExcessiveRepeatRegex().IsMatch(v);
    }

    // ── URL / sitio web ───────────────────────────────────────────────────
    public static bool IsValidUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var s = value.Trim();
        if (!s.Contains("://", StringComparison.Ordinal)) s = "https://" + s;
        return Uri.TryCreate(s, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            && uri.Host.Contains('.', StringComparison.Ordinal)
            && !uri.Host.Contains(' ', StringComparison.Ordinal);
    }

    // ── Teléfono / celular ────────────────────────────────────────────────
    // Celular CO: ^3\d{9}$ (10 dígitos, empieza en 3) | Internacional E.164: ^\+\d{7,15}$.
    [GeneratedRegex(@"^(3\d{9}|\+\d{7,15})$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();

    public static bool IsValidPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var s = Regex.Replace(value, @"[\s\-()]", string.Empty);
        return PhoneRegex().IsMatch(s);
    }

    // ── Dirección colombiana (nomenclatura DIAN, pragmática) ──────────────
    // <Tipo de vía> <Nº vía> # <Nº> - <Nº> [complemento]. Ej. "CL 100 # 13-21".
    [GeneratedRegex(
        @"^(CL|CALLE|KR|CR|CRA|CARRERA|AV|AVENIDA|AC|AK|DG|DIAGONAL|TV|TRANSV|TRANSVERSAL|CQ|CIRCULAR|CV|CIRCUNVALAR|AU|AUTOPISTA|KM|MZ|MANZANA|VRD|VEREDA)\.?\s+\S+.*#\s*\d+\s*-\s*\d+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AddressRegex();

    public static bool IsValidColombianAddress(string? value) =>
        !string.IsNullOrWhiteSpace(value) && AddressRegex().IsMatch(value.Trim());

    // ── Geocoordenadas (rango mundial duro) ───────────────────────────────
    public static bool IsValidLatitude(decimal? value) => !value.HasValue || (value >= -90m && value <= 90m);
    public static bool IsValidLongitude(decimal? value) => !value.HasValue || (value >= -180m && value <= 180m);

    // ── Fecha de nacimiento ───────────────────────────────────────────────
    public static string? BirthDateError(DateOnly? value)
    {
        if (!value.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (value.Value > today) return "La fecha de nacimiento no puede ser futura.";
        if (value.Value < today.AddYears(-120)) return "La fecha de nacimiento no es válida.";
        return null;
    }

    // ── Valor de canal según su tipo ──────────────────────────────────────
    public static bool IsValidChannelValue(string? channelTypeCode, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true; // requerido se valida aparte
        var type = (channelTypeCode ?? string.Empty).Trim().ToUpperInvariant();
        if (type.Contains("EMAIL", StringComparison.Ordinal) || type.Contains("CORREO", StringComparison.Ordinal))
            return value.Contains('@', StringComparison.Ordinal) && value.Contains('.', StringComparison.Ordinal);
        if (type.Contains("WEB", StringComparison.Ordinal) || type.Contains("URL", StringComparison.Ordinal) || type.Contains("SITIO", StringComparison.Ordinal))
            return IsValidUrl(value);
        if (type.Contains("WHATSAPP", StringComparison.Ordinal) || type.Contains("CEL", StringComparison.Ordinal)
            || type.Contains("PHONE", StringComparison.Ordinal) || type.Contains("TEL", StringComparison.Ordinal)
            || type.Contains("MOVIL", StringComparison.Ordinal) || type.Contains("MÓVIL", StringComparison.Ordinal))
            return IsValidPhone(value);
        return true; // tipo no reconocido: sin formato estricto
    }
}
