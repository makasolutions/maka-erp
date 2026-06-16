using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain.V2.Events;

/// <summary>Se abrió una cuenta de crédito para un cliente (SPEC §16). Emitido por <c>CreditAccount.Open</c>.</summary>
public sealed record CreditAssignedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid CreditAccountId,
    Guid CustomerProfileId,
    decimal CupoAsignado,
    string TenantIdValue,
    string? CorrelationId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantIdValue);

/// <summary>Se registró un movimiento en una cuenta de crédito (SPEC §16). Emitido por <c>CreditAccount.AddMovement</c>.</summary>
public sealed record CreditMovementRecordedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid CreditAccountId,
    Guid CreditMovementId,
    CreditMovementType Tipo,
    decimal Monto,
    decimal SaldoResultante,
    string TenantIdValue,
    string? CorrelationId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantIdValue);
