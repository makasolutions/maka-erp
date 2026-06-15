using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.NamingSeries.Contracts.Events;

/// <summary>
/// Integration event público — la serie cruzó el 80% de uso de su rango (ADR-0007).
/// Consumido por Notifications (mail/WhatsApp al CFO) para gestionar renovación de resolución DIAN.
/// </summary>
public sealed record NamingSeriesThresholdReachedIntegrationEvent(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid NamingSeriesId,
    string DocumentType,
    int CurrentValue,
    int Capacity,
    int PercentUsed,
    string? ResolutionNumber
) : IIntegrationEvent;
