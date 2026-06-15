using FSH.Framework.Eventing.Abstractions;

namespace FSH.Modules.NamingSeries.Contracts.Events;

/// <summary>
/// Integration event público — un intento de <c>Allocate</c> falló porque la serie agotó el rango (ADR-0007).
/// Crítico: indica que documentos del <see cref="DocumentType"/> en este tenant NO se están emitiendo.
/// Consumido por Notifications (alerta urgente al CFO/Admin).
/// </summary>
public sealed record NamingSeriesExhaustedIntegrationEvent(
    Guid Id,
    DateTimeOffset OccurredOnUtc,
    string? TenantId,
    string CorrelationId,
    string Source,
    Guid NamingSeriesId,
    string DocumentType,
    int CurrentValue,
    int UpperBound,
    string? ResolutionNumber
) : IIntegrationEvent;
