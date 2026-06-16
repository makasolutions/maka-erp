using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain.V2.Events;

// PR-A: tipos de evento DEFINIDOS. Su emisión se cablea en PR-D (agregado Party).

/// <summary>Se colocó un bloqueo sobre un tercero (SPEC §16).</summary>
public sealed record HoldPlacedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid PartyId,
    Guid HoldId,
    HoldType HoldType,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);

/// <summary>Se liberó un bloqueo de un tercero (SPEC §16).</summary>
public sealed record HoldReleasedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid PartyId,
    Guid HoldId,
    HoldType HoldType,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
