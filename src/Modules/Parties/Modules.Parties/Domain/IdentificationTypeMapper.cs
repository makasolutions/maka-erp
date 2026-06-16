using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Reconciliación canónica entre el código de Tabla Básica de identificación (v1) y el enum
/// <see cref="TipoIdentificacion"/> (v2). Cierra la deuda de reconciliación que PR-A dejó marcada
/// (los marcadores en IdentificationEnums.cs y LegalRepresentative.cs se removieron en PR-D4).
///
/// Mapeo bidireccional ASIMÉTRICO en tolerancia:
/// <list type="bullet">
///   <item><see cref="TryToEnum"/> — código→enum, PARCIAL: tolera entrada sucia (null/desconocido → null),
///         como <c>TaxRegimeMapper</c>. Lo usa el backfill y cualquier lectura de datos v1.</item>
///   <item><see cref="ToCode"/> — enum→código, TOTAL: cada enum tiene un código canónico (salida limpia).
///         Lo usará PR-E al exponer el modelo v2 por el <c>PartyDetailDto.IdentificationTypeCode</c> (string).</item>
/// </list>
///
/// Tabla canónica:
/// <code>
///   CC↔CC · CE↔CE · NIT↔NIT · TI↔TI · NUIP↔NUIP · PASAPORTE↔Pasaporte · NIT_EXT↔NITExtranjero
///   RUT↔RUT · PEP↔PEP  (solo-enum: v1 NO los seedea aún — ver nota abajo)
/// </code>
/// </summary>
public static class IdentificationTypeMapper
{
    // ⚠️ DEUDA VISIBLE (no cabo suelto): "RUT" y "PEP" son los códigos canónicos forward-compatible
    // que el seeder de Tabla Básica de Lookups (LookupsDbInitializer, tabla "IdentificationType")
    // DEBERÍA añadir cuando RUT/PEP entren en uso real. Hoy v1 solo seedea
    // NIT/CC/CE/PASAPORTE/TI/NUIP/NIT_EXT. ToCode devuelve "RUT"/"PEP" de forma anticipada para que
    // el mapeo sea total y reversible; al sembrarlos en Lookups, código↔enum ya cuadra sin cambios.
    public const string RutCode = "RUT";
    public const string PepCode = "PEP";

    /// <summary>Código→enum (parcial): null/desconocido → <c>null</c>. Case-insensitive.</summary>
    public static TipoIdentificacion? TryToEnum(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return code.Trim().ToUpperInvariant() switch
        {
            "CC" => TipoIdentificacion.CC,
            "CE" => TipoIdentificacion.CE,
            "NIT" => TipoIdentificacion.NIT,
            "TI" => TipoIdentificacion.TI,
            "NUIP" => TipoIdentificacion.NUIP,
            "PASAPORTE" => TipoIdentificacion.Pasaporte,
            "NIT_EXT" => TipoIdentificacion.NITExtranjero,
            RutCode => TipoIdentificacion.RUT,
            PepCode => TipoIdentificacion.PEP,
            _ => null,
        };
    }

    /// <summary>Enum→código (total): cada valor tiene un código canónico de Tabla Básica.</summary>
    public static string ToCode(TipoIdentificacion tipo) => tipo switch
    {
        TipoIdentificacion.CC => "CC",
        TipoIdentificacion.CE => "CE",
        TipoIdentificacion.NIT => "NIT",
        TipoIdentificacion.TI => "TI",
        TipoIdentificacion.NUIP => "NUIP",
        TipoIdentificacion.Pasaporte => "PASAPORTE",
        TipoIdentificacion.NITExtranjero => "NIT_EXT",
        TipoIdentificacion.RUT => RutCode,
        TipoIdentificacion.PEP => PepCode,
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "TipoIdentificacion no soportado."),
    };
}
