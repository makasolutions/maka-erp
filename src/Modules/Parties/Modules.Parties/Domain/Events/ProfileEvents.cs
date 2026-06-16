using FSH.Framework.Core.Domain;

namespace FSH.Modules.Parties.Domain.Events;

// PR-A: tipos de evento DEFINIDOS. Su emisión se cablea en PR-D, cuando los Profiles
// entren al agregado Party (que será quien acumule y despache los domain events).

/// <summary>Se activó una faceta comercial de un tercero (SPEC §16).</summary>
public sealed record ProfileActivatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid PartyId,
    string ProfileType,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);

/// <summary>Se desactivó (soft) una faceta comercial de un tercero (SPEC §16).</summary>
public sealed record ProfileDeactivatedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid PartyId,
    string ProfileType,
    string? CorrelationId = null,
    string? TenantId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantId);
