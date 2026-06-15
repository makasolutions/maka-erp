namespace FSH.Framework.Eventing;

/// <summary>
/// Configuration options for the eventing building block.
/// </summary>
public sealed class EventingOptions
{
    /// <summary>
    /// Provider for the event bus implementation. Supported: "InMemory", "RabbitMQ".
    /// </summary>
    public string Provider { get; set; } = "InMemory";

    /// <summary>
    /// Batch size for outbox dispatching.
    /// </summary>
    public int OutboxBatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum number of retries before an outbox message is marked as dead.
    /// </summary>
    public int OutboxMaxRetries { get; set; } = 5;

    /// <summary>
    /// Whether inbox-based idempotent handling is enabled.
    /// </summary>
    public bool EnableInbox { get; set; } = true;

    /// <summary>
    /// Interval in seconds for the outbox dispatcher background service.
    /// Set to 0 to disable the background service (use Hangfire instead).
    /// </summary>
    public int OutboxDispatchIntervalSeconds { get; set; } = 10;

    /// <summary>
    /// Whether to use the hosted service for outbox dispatching.
    /// If false, you should configure Hangfire or another scheduler.
    /// </summary>
    public bool UseHostedServiceDispatcher { get; set; } = true;

    /// <summary>
    /// ADR-0005 — routing por evento durante la migración Wolverine. Llave: nombre del tipo
    /// del integration event (<c>typeof(T).Name</c>). Valor: <c>"Wolverine"</c> o <c>"Legacy"</c>.
    /// Default por omisión: <c>"Legacy"</c> (bus propio). Se cambia a <c>"Wolverine"</c> por
    /// evento conforme migran; el borrado del bus propio queda para Fase 5.
    /// </summary>
    public Dictionary<string, string> IntegrationEventRouting { get; set; } = [];
}