namespace FSH.Modules.Parties.Domain.V2.Exceptions;

#pragma warning disable CA1032 // Excepciones de dominio con causa única; no requieren los ctors estándar.

/// <summary>El período de un <c>PartyHold</c> es inválido (liberación anterior al inicio).</summary>
public sealed class InvalidHoldPeriodException(DateOnly inicio, DateOnly liberacion)
    : InvalidOperationException($"La fecha de liberación ({liberacion:O}) no puede ser anterior a la fecha de inicio ({inicio:O}).")
{
    public DateOnly FechaInicio { get; } = inicio;
    public DateOnly FechaLiberacion { get; } = liberacion;
}

/// <summary>Un movimiento de crédito viola una invariante (monto no positivo, asignación duplicada, etc.).</summary>
public sealed class InvalidCreditMovementException(string message) : InvalidOperationException(message);

#pragma warning restore CA1032
