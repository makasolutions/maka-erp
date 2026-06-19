using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Framework.Persistence;

/// <summary>
/// CAPA 3 — factory centralizado para que un handler que ESCRIBE en una base tenant-aislada
/// (p.ej. un handler de integration event de Wolverine) obtenga un DbContext bindeado al tenant
/// CORRECTO.
///
/// Por qué existe: en el pipeline de Wolverine, el frame de transacción EF construye el DbContext
/// del handler ANTES de que el <c>TenantContextMiddleware</c> setee el tenant Finbuckle. Y Finbuckle
/// captura el <c>TenantInfo</c> en CONSTRUCCIÓN (propiedad read-only, sin re-resolución pública), así
/// que el DbContext INYECTADO por parámetro queda con tenant null → <c>MultiTenantException</c> al
/// <c>SaveChanges</c>. Este factory abre un scope FRESCO, setea el tenant, y resuelve el DbContext
/// REGISTRADO ahí (opciones/conexión correctas + enrolamiento Wolverine) DESPUÉS del set → captura el
/// tenant correcto en construcción.
///
/// 🚩 FOOTGUN (sumar a la checklist de "handler nuevo", junto al IncludeType de discovery): cada
/// handler que ESCRIBA en una base tenant-aislada debe usar este factory, NO el DbContext inyectado
/// por parámetro (que el frame EF-tx construye con tenant null). Un miss es bug silencioso en runtime.
///
/// 🧾 DEUDA ESTRUCTURAL: cuando haya varios handlers tenant-writing, evaluar un puente accessor
/// Finbuckle↔Wolverine (un lugar, automático, sin checklist per-handler). Migrar = cambiar ESTE
/// factory, no N handlers — por eso el patrón vive centralizado desde el principio.
/// </summary>
public static class TenantScopedDbContext
{
    /// <summary>
    /// Abre un scope con <paramref name="tenantId"/> seteado en el Finbuckle context y resuelve el
    /// <typeparamref name="TContext"/> REGISTRADO ahí. El DbContext, construido tras el set, captura el
    /// tenant correcto. Disponé el lease (<c>await using</c>) para cerrar el scope.
    /// </summary>
    public static TenantScopedDbContextLease<TContext> Create<TContext>(
        IServiceScopeFactory scopeFactory, string tenantId)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(scopeFactory);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>().MultiTenantContext =
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo(tenantId, tenantId));
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        return new TenantScopedDbContextLease<TContext>(scope, context);
    }
}

/// <summary>
/// Lease del <typeparamref name="TContext"/> tenant-bindeado y su scope. Disponer cierra el scope.
/// </summary>
public sealed class TenantScopedDbContextLease<TContext> : IAsyncDisposable
    where TContext : DbContext
{
    private readonly IServiceScope _scope;

    /// <summary>El <typeparamref name="TContext"/> registrado, bindeado al tenant indicado.</summary>
    public TContext Context { get; }

    internal TenantScopedDbContextLease(IServiceScope scope, TContext context)
    {
        _scope = scope;
        Context = context;
    }

    public async ValueTask DisposeAsync()
    {
        if (_scope is IAsyncDisposable asyncScope)
        {
            await asyncScope.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _scope.Dispose();
        }
    }
}
