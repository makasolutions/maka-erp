using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain.V2.Credit;

/// <summary>
/// Movimiento inmutable en la cuenta de crédito de un cliente (SPEC §9, log inmutable).
///
/// Inmutabilidad estructural: setters privados, sin métodos mutadores, y el ÚNICO punto de
/// creación es el factory <see cref="Record"/> (constructor privado para EF). Una vez creado,
/// un movimiento nunca cambia — el historial es append-only (doctrina constitución §4).
/// </summary>
public sealed class CreditMovement : BaseEntity<Guid>
{
    public Guid CreditAccountId { get; private set; }
    public CreditMovementType Tipo { get; private set; }
    public decimal Monto { get; private set; }            // signo aplicado (+ o −) sobre el saldo
    public decimal SaldoResultante { get; private set; }  // cupo disponible tras el movimiento
    public Guid? DocumentoReferenciaId { get; private set; }
    public string? Motivo { get; private set; }
    public DateTimeOffset FechaUtc { get; private set; }
    public string RegistradoPor { get; private set; } = default!;

    private CreditMovement() { }

    /// <summary>Único punto de creación. Llamado exclusivamente por <see cref="CreditAccount"/>.</summary>
    internal static CreditMovement Record(
        Guid creditAccountId,
        CreditMovementType tipo,
        decimal montoConSigno,
        decimal saldoResultante,
        string registradoPor,
        DateTimeOffset fechaUtc,
        Guid? documentoReferenciaId,
        string? motivo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registradoPor);
        return new CreditMovement
        {
            Id = Guid.CreateVersion7(),
            CreditAccountId = creditAccountId,
            Tipo = tipo,
            Monto = montoConSigno,
            SaldoResultante = saldoResultante,
            RegistradoPor = registradoPor.Trim(),
            FechaUtc = fechaUtc,
            DocumentoReferenciaId = documentoReferenciaId,
            Motivo = motivo?.Trim(),
        };
    }
}
