using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain.V2;

/// <summary>
/// Representante legal de una persona jurídica (modelo v2, SPEC §5). Value object inmutable.
///
/// Divergencia v1↔v2: usa <see cref="Contracts.Enums.TipoIdentificacion"/> (enum) en lugar
/// de los códigos de Tabla Básica de v1. La reconciliación enum↔código se resolvió en PR-D4
/// (ver <c>IdentificationTypeMapper</c>). PR-D4: incrustado como owned VO nullable del Party.
/// </summary>
public sealed record LegalRepresentative
{
    public string Nombres { get; init; } = default!;
    public string Apellidos { get; init; } = default!;
    public TipoIdentificacion TipoIdentificacion { get; init; }
    public string NumeroIdentificacion { get; init; } = default!;
    public string? Telefono { get; init; }
    public string? TelefonoExtension { get; init; }
    public string? Celular { get; init; }
    public string? Email { get; init; }
    public bool EsPEP { get; init; }

    public static LegalRepresentative Create(
        string nombres,
        string apellidos,
        TipoIdentificacion tipoIdentificacion,
        string numeroIdentificacion,
        string? telefono = null,
        string? telefonoExtension = null,
        string? celular = null,
        string? email = null,
        bool esPEP = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombres);
        ArgumentException.ThrowIfNullOrWhiteSpace(apellidos);
        ArgumentException.ThrowIfNullOrWhiteSpace(numeroIdentificacion);
        return new LegalRepresentative
        {
            Nombres = nombres.Trim(),
            Apellidos = apellidos.Trim(),
            TipoIdentificacion = tipoIdentificacion,
            NumeroIdentificacion = numeroIdentificacion.Trim(),
            Telefono = telefono?.Trim(),
            TelefonoExtension = telefonoExtension?.Trim(),
            Celular = celular?.Trim(),
            Email = email?.Trim(),
            EsPEP = esPEP,
        };
    }
}
