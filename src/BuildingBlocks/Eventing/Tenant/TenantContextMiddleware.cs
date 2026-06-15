using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Wolverine;

namespace FSH.Framework.Eventing.Tenant;

/// <summary>
/// INV-9 estructural (ADR-0001/0005, Fase 3) — middleware Wolverine incoming que
/// restaura el Finbuckle context desde <c>envelope.TenantId</c> ANTES de ejecutar el
/// handler. Reemplaza el patrón manual de
/// <c>WebhookFanoutHandler</c> y <c>MentionedInChannelIntegrationEventHandler</c>
/// donde cada handler tenía que setear el accessor manualmente.
///
/// Wolverine descubre el método <c>Before</c> por convención y genera el frame
/// correspondiente en el pipeline del handler. La firma debe aceptar dependencias
/// inyectables (incluido <see cref="Envelope"/> y el setter del scope).
///
/// Registrado en <c>opts.Policies.AddMiddleware&lt;TenantContextMiddleware&gt;()</c>.
/// </summary>
#pragma warning disable CA1052, S1118 // Wolverine descubre la clase via reflection y
// exige (a) un tipo no-estático como argumento genérico a IPolicies.AddMiddleware<T>,
// (b) constructor público para instanciar la clase (aunque el método Before sea
// estático y no use estado de instancia). Patrón canónico Wolverine.
public sealed class TenantContextMiddleware
#pragma warning restore CA1052, S1118
{
    public static void Before(Envelope envelope, IMultiTenantContextSetter setter)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(setter);

        if (string.IsNullOrEmpty(envelope.TenantId)) return;

        var info = new AppTenantInfo(envelope.TenantId, envelope.TenantId);
        setter.MultiTenantContext = new MultiTenantContext<AppTenantInfo>(info);
    }
}
