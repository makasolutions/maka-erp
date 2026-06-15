using System.Collections.Concurrent;
using System.Net.Http.Json;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Identity.Contracts.Events;
using FSH.Modules.Identity.Contracts.v1.Users.RegisterUser;
using Integration.Tests.Infrastructure;
using Microsoft.Extensions.Hosting;
using Wolverine;
using Wolverine.Tracking;

namespace Integration.Tests.Tests.Platform;

/// <summary>
/// Fase 2 (ADR-0001/0005) — primer publicador real migrado a Wolverine.
///
/// Verifica end-to-end que el registro de un usuario en Identity:
///   1. Produce un envelope <see cref="UserRegisteredIntegrationEvent"/> capturado por
///      <c>tracked.Sent</c> (Wolverine vio el publish).
///   2. Se entrega vía RabbitMQ (Testcontainer activo) al exchange
///      <c>maka.wolverine.identity.events</c>.
///   3. Es recibido por un consumer test-only registrado vía
///      <c>ConfigureWolverine</c> additive (mismo patrón Fase 1).
///   4. Lleva <c>Envelope.TenantId</c> propagado desde el Finbuckle context (INV-9 estructural).
///
/// La invocación va por HTTP a <c>POST api/v1/identity/self-register</c> — endpoint anónimo
/// que recibe el tenant en el header. Esto activa el middleware Finbuckle estándar y produce
/// un scope completo con todos los servicios resueltos (UserManager con su tenant, etc.).
///
/// Este test es la VALIDACIÓN OBSERVABLE de ADR-0001 con un publicador real. Si pasa, la
/// 5ª capa documentada en wolverine-phase1-followups.md queda cerrada como subproducto.
/// </summary>
[Collection(FshCollectionDefinition.Name)]
public sealed class WolverineUserRegisteredE2ETests
{
    private readonly FshWebApplicationFactory _factory;

    public WolverineUserRegisteredE2ETests(FshWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserRegistration_Should_Publish_Envelope_And_Deliver_Via_RabbitMq()
    {
        var collector = _factory.Services.GetRequiredService<UserRegisteredCollector>();
        collector.Clear();

        var marker = $"e2e-{Guid.NewGuid():N}"[..16];
        var email = $"phase2-{marker}@maka.test";

        var command = new RegisterUserCommand
        {
            FirstName = "Phase2",
            LastName = "Tester",
            Email = email,
            UserName = $"phase2_{marker}"[..20],
            Password = "Phase2Pass!1",
            ConfirmPassword = "Phase2Pass!1",
            PhoneNumber = "+573001234567",
        };

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(MultitenancyConstants.Identifier, TestConstants.RootTenantId);

        var host = _factory.Services.GetRequiredService<IHost>();

        Func<IMessageContext, Task> action = async _ =>
        {
            var response = await client.PostAsJsonAsync("api/v1/identity/self-register", command);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException(
                    $"self-register endpoint debe responder 2xx; respondió {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }
        };

        var tracked = await host.TrackActivity()
            .Timeout(TimeSpan.FromSeconds(30))
            .IncludeExternalTransports()
            .ExecuteAndWaitAsync(action);

        // Aserto 1 — Wolverine vio el publish (envelope capturado por el tracker en outgoing).
        var sent = tracked.Sent.SingleEnvelope<UserRegisteredIntegrationEvent>();
        sent.ShouldNotBeNull("Wolverine debió emitir un envelope para UserRegisteredIntegrationEvent");
        sent.TenantId.ShouldBe(TestConstants.RootTenantId,
            "INV-9 — TenantId del envelope debe venir del Finbuckle context del registro");

        // Aserto 2 — el envelope fue RECIBIDO por el consumer test-only via RabbitMQ.
        // Si llegó por queue local in-memory el destination scheme sería "local://"; al venir
        // por RabbitMQ debe ser "rabbitmq://" (verificación de tránsito real).
        var receivedEnvelope = tracked.Received.SingleEnvelope<UserRegisteredIntegrationEvent>();
        receivedEnvelope.ShouldNotBeNull("El consumer test-only debió recibir el evento");
        receivedEnvelope.Destination?.Scheme.ShouldBe("rabbitmq",
            "el evento debe transitar por RabbitMQ, no por queue local");

        // Aserto 3 — el payload llegó intacto al consumer.
        collector.Received.Count.ShouldBe(1);
        collector.Received[0].Email.ShouldBe(email);
        collector.Received[0].TenantId.ShouldBe(TestConstants.RootTenantId);

        // Aserto 4 (Fase 3 Paso 3) — INV-9 estructural. El TenantContextMiddleware de
        // Wolverine debió restaurar el Finbuckle ITenantInfo desde envelope.TenantId
        // ANTES de invocar al consumer. Si el middleware no está activo o no se ejecuta
        // antes del handler, el AmbientTenantId queda null → test rojo.
        collector.AmbientTenantId.ShouldBe(TestConstants.RootTenantId,
            "INV-9 — TenantContextMiddleware debe poblar el Finbuckle context antes del handler");
    }

}

/// <summary>
/// Sink singleton donde el consumer test-only graba los eventos recibidos para que el test
/// los pueda asertar. Vida del factory (un test → un Clear() al inicio).
/// </summary>
public sealed class UserRegisteredCollector
{
    private readonly ConcurrentBag<UserRegisteredIntegrationEvent> _received = [];

    public IReadOnlyList<UserRegisteredIntegrationEvent> Received => [.. _received];

    /// <summary>
    /// Tenant ambient visto por el handler en el momento de ejecutarse (poblado por
    /// el <c>TenantContextMiddleware</c> INV-9 de Wolverine). Si el middleware no
    /// está activo este valor queda en <c>null</c> → test rojo (verificación
    /// estructural Fase 3 Paso 3).
    /// </summary>
    public string? AmbientTenantId { get; set; }

    public void Add(UserRegisteredIntegrationEvent evt) => _received.Add(evt);

    public void Clear()
    {
        _received.Clear();
        AmbientTenantId = null;
    }
}

/// <summary>
/// Consumer test-only. Discoverable cuando el factory llama
/// <c>opts.Discovery.IncludeType(typeof(UserRegisteredE2EConsumer))</c> via
/// <c>ConfigureWolverine</c>. Solo graba en el collector — no muta nada del sistema.
/// </summary>
public static class UserRegisteredE2EConsumer
{
    public static void Handle(
        UserRegisteredIntegrationEvent evt,
        UserRegisteredCollector collector,
        Finbuckle.MultiTenant.Abstractions.IMultiTenantContextAccessor<FSH.Framework.Shared.Multitenancy.AppTenantInfo> tenantAccessor)
    {
        ArgumentNullException.ThrowIfNull(evt);
        ArgumentNullException.ThrowIfNull(collector);
        // INV-9 — captura el tenant ambient para asertar que el TenantContextMiddleware
        // de Wolverine restauró el Finbuckle context ANTES de invocar este handler.
        collector.AmbientTenantId = tenantAccessor?.MultiTenantContext?.TenantInfo?.Id;
        collector.Add(evt);
    }
}
