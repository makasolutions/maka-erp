using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Domain.V2.Events;
using FSH.Modules.Parties.Domain.V2.Exceptions;

namespace FSH.Modules.Parties.Domain.V2.Credit;

/// <summary>
/// Cuenta de crédito de un cliente (SPEC §9). Agregado raíz: posee el historial inmutable de
/// <see cref="CreditMovement"/> y mantiene la invariante del saldo disponible.
///
/// El crédito no es un número, es una cuenta con movimientos (confirmado por Frappe
/// <c>credit_limits</c> y directriz de Juan). Cada cambio de cupo genera un movimiento inmutable;
/// <c>SaldoResultante</c> se recalcula en cadena: <c>saldo[n] = saldo[n-1] + montoConSigno</c>.
///
/// Convención de signo por tipo:
/// <list type="bullet">
///   <item>AsignacionInicial → fija el baseline (cupo y saldo = monto)</item>
///   <item>Aumento, Liberacion → suman al saldo disponible</item>
///   <item>Reduccion, Consumo, Bloqueo → restan del saldo disponible</item>
/// </list>
/// Aumento/Reduccion además ajustan <see cref="CupoAsignado"/>; Consumo/Liberacion/Bloqueo no.
/// </summary>
public sealed class CreditAccount : AggregateRoot<Guid>
{
    public Guid CustomerProfileId { get; private set; }
    public string TenantId { get; private set; } = default!;
    public decimal CupoAsignado { get; private set; }
    public decimal SaldoDisponible { get; private set; }
    public string MonedaId { get; private set; } = "COP";
    public int DiasCredito { get; private set; }
    public Guid? FormaPagoId { get; private set; }
    public bool EstaActivo { get; private set; }
    public DateOnly? FechaAprobacion { get; private set; }
    public string? AprobadoPor { get; private set; }

    private readonly List<CreditMovement> _movements = [];
    public IReadOnlyList<CreditMovement> Movements => _movements.AsReadOnly();

    private CreditAccount() { }

    /// <summary>Abre la cuenta con un cupo inicial y registra el movimiento <c>AsignacionInicial</c>.</summary>
    public static CreditAccount Open(
        Guid customerProfileId,
        string tenantId,
        decimal cupoInicial,
        string registradoPor,
        string monedaId = "COP",
        int diasCredito = 0,
        Guid? formaPagoId = null,
        DateOnly? fechaAprobacion = null,
        string? aprobadoPor = null,
        DateTimeOffset? now = null)
    {
        if (customerProfileId == Guid.Empty) throw new ArgumentException("CustomerProfileId requerido.", nameof(customerProfileId));
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(registradoPor);
        if (cupoInicial < 0) throw new InvalidCreditMovementException("El cupo inicial no puede ser negativo.");

        var ts = now ?? DateTimeOffset.UtcNow;
        var account = new CreditAccount
        {
            Id = Guid.CreateVersion7(),
            CustomerProfileId = customerProfileId,
            TenantId = tenantId.Trim(),
            MonedaId = string.IsNullOrWhiteSpace(monedaId) ? "COP" : monedaId.Trim(),
            DiasCredito = diasCredito,
            FormaPagoId = formaPagoId,
            EstaActivo = true,
            FechaAprobacion = fechaAprobacion,
            AprobadoPor = aprobadoPor?.Trim(),
            CupoAsignado = cupoInicial,
            SaldoDisponible = cupoInicial,
        };

        var mov = CreditMovement.Record(
            account.Id, CreditMovementType.AsignacionInicial, cupoInicial, cupoInicial,
            registradoPor.Trim(), ts, documentoReferenciaId: null, motivo: "Asignación inicial de cupo");
        account._movements.Add(mov);

        account.AddDomainEvent(new CreditAssignedDomainEvent(
            Guid.NewGuid(), ts, account.Id, customerProfileId, cupoInicial, account.TenantId));
        account.AddDomainEvent(new CreditMovementRecordedDomainEvent(
            Guid.NewGuid(), ts, account.Id, mov.Id, mov.Tipo, mov.Monto, mov.SaldoResultante, account.TenantId));

        return account;
    }

    /// <summary>
    /// Registra un movimiento (excepto <c>AsignacionInicial</c>, exclusivo de <see cref="Open"/>) y
    /// recalcula <see cref="SaldoDisponible"/>. <paramref name="montoAbsoluto"/> es positivo; el signo
    /// se deriva del <paramref name="tipo"/>.
    /// </summary>
    public CreditMovement AddMovement(
        CreditMovementType tipo,
        decimal montoAbsoluto,
        string registradoPor,
        Guid? documentoReferenciaId = null,
        string? motivo = null,
        DateTimeOffset? now = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registradoPor);
        if (tipo == CreditMovementType.AsignacionInicial)
            throw new InvalidCreditMovementException("AsignacionInicial solo se registra al abrir la cuenta.");
        if (montoAbsoluto <= 0)
            throw new InvalidCreditMovementException("El monto del movimiento debe ser positivo.");

        decimal montoConSigno = tipo switch
        {
            CreditMovementType.Aumento or CreditMovementType.Liberacion => montoAbsoluto,
            CreditMovementType.Reduccion or CreditMovementType.Consumo or CreditMovementType.Bloqueo => -montoAbsoluto,
            _ => throw new InvalidCreditMovementException($"Tipo de movimiento no soportado: {tipo}."),
        };

        SaldoDisponible += montoConSigno;
        if (tipo is CreditMovementType.Aumento) CupoAsignado += montoAbsoluto;
        else if (tipo is CreditMovementType.Reduccion) CupoAsignado -= montoAbsoluto;

        var ts = now ?? DateTimeOffset.UtcNow;
        var mov = CreditMovement.Record(
            Id, tipo, montoConSigno, SaldoDisponible, registradoPor.Trim(), ts, documentoReferenciaId, motivo);
        _movements.Add(mov);

        AddDomainEvent(new CreditMovementRecordedDomainEvent(
            Guid.NewGuid(), ts, Id, mov.Id, mov.Tipo, mov.Monto, mov.SaldoResultante, TenantId));

        return mov;
    }

    public void Deactivate() => EstaActivo = false;
    public void Reactivate() => EstaActivo = true;
}
