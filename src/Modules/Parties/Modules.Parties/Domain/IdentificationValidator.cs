using System.Globalization;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Validación local (sin red) de identificaciones colombianas: dígito de
/// verificación del NIT (algoritmo DIAN) y formato/longitud por tipo de documento.
/// Fuente de verdad del backend; el front replica el cálculo del DV solo para UX.
/// </summary>
public static class IdentificationValidator
{
    // Pesos DIAN aplicados de derecha a izquierda; suma mod 11.
    private static readonly int[] Weights =
        [3, 7, 13, 17, 19, 23, 29, 37, 41, 43, 47, 53, 59, 67, 71];

    /// <summary>Calcula el DV de un NIT numérico, o null si no hay dígitos.</summary>
    public static int? NitVerificationDigit(string? identification)
    {
        if (string.IsNullOrWhiteSpace(identification)) return null;
        var digits = new string(identification.Where(char.IsDigit).ToArray());
        if (digits.Length == 0) return null;

        int sum = 0;
        for (int i = 0; i < digits.Length; i++)
        {
            int d = digits[digits.Length - 1 - i] - '0';
            sum += d * (i < Weights.Length ? Weights[i] : 0);
        }
        int rem = sum % 11;
        return rem < 2 ? rem : 11 - rem;
    }

    /// <summary>
    /// Valida número + (para NIT) dígito de verificación según el tipo de documento.
    /// Devuelve un mensaje de error o null si es válido.
    /// </summary>
    public static string? Validate(string? identificationTypeCode, string? number, int? verificationDigit)
    {
        string type = (identificationTypeCode ?? string.Empty).Trim().ToUpperInvariant();
        string raw = (number ?? string.Empty).Trim();
        if (raw.Length == 0) return "El número de identificación es obligatorio.";

        switch (type)
        {
            case "NIT":
            case "NIT_EXT":
            {
                var digits = new string(raw.Where(char.IsDigit).ToArray());
                if (digits.Length is < 5 or > 15)
                    return "El NIT debe tener entre 5 y 15 dígitos.";
                // El DV no se valida aquí: es determinístico y se calcula/corrige
                // automáticamente al crear/actualizar (ver ResolveVerificationDigit).
                return null;
            }
            case "CC":
            case "TI":
            case "NUIP":
            {
                if (!raw.All(char.IsDigit))
                    return "La cédula/identificación debe ser numérica.";
                if (raw.Length is < 4 or > 11)
                    return "La cédula debe tener entre 4 y 11 dígitos.";
                return null;
            }
            case "CE":
            {
                if (raw.Length is < 3 or > 15)
                    return "La cédula de extranjería tiene una longitud inválida.";
                return null;
            }
            case "PASAPORTE":
            {
                if (raw.Length is < 5 or > 20)
                    return "El número de pasaporte tiene una longitud inválida.";
                return null;
            }
            default:
                return null; // tipos no reconocidos: sin validación estricta
        }
    }

    /// <summary>
    /// Devuelve el DV correcto a almacenar: para NIT lo calcula (determinístico,
    /// ignorando el provisto); para otros tipos respeta el provisto.
    /// </summary>
    public static int? ResolveVerificationDigit(string? identificationTypeCode, string? number, int? provided)
    {
        string type = (identificationTypeCode ?? string.Empty).Trim().ToUpperInvariant();
        if (type is "NIT" or "NIT_EXT")
            return NitVerificationDigit(number) ?? provided;
        return provided;
    }

    /// <summary>Normaliza un número (quita separadores) para comparación/almacenamiento.</summary>
    public static string NormalizeNumber(string number) =>
        new(number.Where(c => char.IsLetterOrDigit(c)).ToArray());

    internal static string Describe(int value) => value.ToString(CultureInfo.InvariantCulture);
}
