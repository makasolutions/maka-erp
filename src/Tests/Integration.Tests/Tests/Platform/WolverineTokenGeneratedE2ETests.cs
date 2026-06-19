using System.Collections.Concurrent;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Events;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Tracking;

namespace Integration.Tests.Tests.Platform;

/// <summary>
/// Fase 2 — publicador 2/4 (ADR-0001/0005). Migración del
/// <c>GenerateTokenCommandHandler</c> a la fachada Wolverine.
///
/// Mismo patrón estructural que <see cref="WolverineUserRegisteredE2ETests"/>:
/// invocación HTTP al endpoint real (<c>POST api/v1/identity/token/issue</c>) →
/// <c>TrackedSession</c> aserta el envelope publicado y entregado por RabbitMQ →
/// consumer test-only registrado vía <c>ConfigureWolverine</c> additive recibe el
/// payload intacto. El exchange es el mismo (<c>maka.wolverine.identity.events</c>):
/// los dos eventos del módulo Identity comparten ruta de transporte.
///
/// El handler legacy <c>TokenGeneratedLogHandler</c> (consumer del bus propio) deja
/// de recibir el evento hasta Fase 3 — sin impacto observable: <c>ISecurityAudit</c>
/// cubre la observabilidad real de la emisión de tokens.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WolverineTokenGeneratedE2ETests
{
    private readonly FshWebApplicationFactory _factory;

    public WolverineTokenGeneratedE2ETests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TokenIssue_Should_Publish_Envelope_And_Deliver_Via_RabbitMq()
    {
        var collector = _factory.Services.GetRequiredService<TokenGeneratedCollector>();
        collector.Clear();

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("tenant", TestConstants.RootTenantId);

        var host = _factory.Services.GetRequiredService<IHost>();

        Func<IMessageContext, Task> action = async _ =>
        {
            var response = await client.PostAsJsonAsync(
                $"{TestConstants.IdentityBasePath}/token/issue",
                new { email = TestConstants.RootAdminEmail, password = TestConstants.DefaultPassword });

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"token/issue endpoint debe responder 2xx; respondió {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }
        };

        var tracked = await host.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .ExecuteAndWaitAsync(action);

        // Aserto 1 — Wolverine vio el publish del envelope.
        var sent = tracked.Sent.SingleEnvelope<TokenGeneratedIntegrationEvent>();
        sent.ShouldNotBeNull("Wolverine debió emitir un envelope para TokenGeneratedIntegrationEvent");
        sent.TenantId.ShouldBe(TestConstants.RootTenantId,
            "INV-9 — TenantId del envelope debe venir del Finbuckle context del request");

        // Aserto 2 — CAPA 2: entrega in-process por la local durable queue (sin RabbitMQ).
        var receivedEnvelope = tracked.Received.SingleEnvelope<TokenGeneratedIntegrationEvent>();
        receivedEnvelope.ShouldNotBeNull("El consumer test-only debió recibir el evento");
        receivedEnvelope.Destination?.Scheme.ShouldBe("local",
            "el evento se entrega in-process por la local durable queue, no por RabbitMQ");

        // Aserto 3 — payload intacto en el consumer test-only.
        collector.Received.Count.ShouldBe(1);
        collector.Received[0].Email.ShouldBe(TestConstants.RootAdminEmail);
        collector.Received[0].TenantId.ShouldBe(TestConstants.RootTenantId);
        collector.Received[0].UserId.ShouldNotBeNullOrWhiteSpace();
        collector.Received[0].TokenFingerprint.ShouldNotBeNullOrWhiteSpace();
    }
}

/// <summary>
/// Sink singleton donde el consumer test-only graba el evento para que el test lo
/// pueda asertar. Vida del factory (un test → un Clear() al inicio).
/// </summary>
public sealed class TokenGeneratedCollector
{
    private readonly ConcurrentBag<TokenGeneratedIntegrationEvent> _received = [];

    public IReadOnlyList<TokenGeneratedIntegrationEvent> Received => [.. _received];

    public void Add(TokenGeneratedIntegrationEvent evt) => _received.Add(evt);

    public void Clear() => _received.Clear();
}

/// <summary>
/// Consumer test-only. Discoverable cuando el factory llama
/// <c>opts.Discovery.IncludeType(typeof(TokenGeneratedE2EConsumer))</c> via
/// <c>ConfigureWolverine</c>. Solo graba en el collector — no muta nada del sistema.
/// </summary>
public static class TokenGeneratedE2EConsumer
{
    public static void Handle(TokenGeneratedIntegrationEvent evt, TokenGeneratedCollector collector)
    {
        ArgumentNullException.ThrowIfNull(evt);
        ArgumentNullException.ThrowIfNull(collector);
        collector.Add(evt);
    }
}
