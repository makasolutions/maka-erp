using FSH.Framework.Core.Domain;

namespace FSH.Modules.NamingSeries.Domain;

/// <summary>
/// Domain event interno — la serie cruzó el 80% de uso de su rango. El allocator lo traduce
/// a integration event público (<c>NamingSeriesThresholdReachedIntegrationEvent</c>) para
/// que consumidores externos (Notifications/Hangfire) alerten al CFO. Idempotente: el agregado
/// setea <c>Notified80Pct = true</c> y NO vuelve a emitir mientras siga vivo.
/// </summary>
public sealed record NamingSeriesThresholdReachedDomainEvent(
    Guid EventId,
    DateTimeOffset OccurredOnUtc,
    Guid NamingSeriesId,
    string TenantIdValue,
    string DocumentType,
    int CurrentValue,
    int Capacity,
    string? CorrelationId = null
) : DomainEvent(EventId, OccurredOnUtc, CorrelationId, TenantIdValue);
