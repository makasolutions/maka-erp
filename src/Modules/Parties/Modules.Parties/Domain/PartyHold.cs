using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.Exceptions;

namespace FSH.Modules.Parties.Domain;

/// <summary>
/// Bloqueo de un tercero (patrón Frappe Block/Hold, SPEC §10). Reemplaza el booleano
/// <c>PermitirVenta</c> de v1: un tercero puede bloquearse parcialmente (por tipo) y con vigencia.
///
/// PR-A: entidad independiente con <c>PartyId</c> Guid (sin nav property en Party). La emisión de
/// <c>HoldPlaced/HoldReleased</c> se cablea en PR-D, cuando Party posea la colección de holds.
/// </summary>
public sealed class PartyHold : BaseEntity<Guid>
{
    public Guid PartyId { get; private set; }
    public HoldType HoldType { get; private set; }
    public string Motivo { get; private set; } = default!;
    public DateOnly FechaInicio { get; private set; }
    public DateOnly? FechaLiberacion { get; private set; }  // null = indefinido (Frappe)
    public bool EstaActivo { get; private set; }
    public string CreadoPor { get; private set; } = default!;

    private PartyHold() { }

    public static PartyHold Place(
        Guid partyId,
        HoldType holdType,
        string motivo,
        DateOnly fechaInicio,
        string creadoPor,
        DateOnly? fechaLiberacion = null)
    {
        if (partyId == Guid.Empty) throw new ArgumentException("PartyId requerido.", nameof(partyId));
        ArgumentException.ThrowIfNullOrWhiteSpace(motivo);
        ArgumentException.ThrowIfNullOrWhiteSpace(creadoPor);
        if (fechaLiberacion is { } fin && fin < fechaInicio)
            throw new InvalidHoldPeriodException(fechaInicio, fin);

        return new PartyHold
        {
            Id = Guid.CreateVersion7(),
            PartyId = partyId,
            HoldType = holdType,
            Motivo = motivo.Trim(),
            FechaInicio = fechaInicio,
            FechaLiberacion = fechaLiberacion,
            EstaActivo = true,
            CreadoPor = creadoPor.Trim(),
        };
    }

    /// <summary>
    /// True si este hold bloquea operaciones del <paramref name="tipo"/> dado en la
    /// <paramref name="fecha"/> indicada: debe estar activo, cubrir el tipo (coincidencia exacta o
    /// <see cref="HoldType.Todo"/>), y la fecha debe caer en [inicio, liberación] (liberación nula = indefinido).
    /// </summary>
    public bool IsActiveOn(DateOnly fecha, HoldType tipo)
    {
        if (!EstaActivo) return false;
        if (HoldType != HoldType.Todo && HoldType != tipo) return false;
        if (fecha < FechaInicio) return false;
        if (FechaLiberacion is { } fin && fecha > fin) return false;
        return true;
    }

    public void Release()
    {
        EstaActivo = false;
        if (FechaLiberacion is null) FechaLiberacion = DateOnly.FromDateTime(DateTime.UtcNow);
    }
}
