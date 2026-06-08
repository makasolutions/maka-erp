using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using Microsoft.Extensions.DependencyInjection;

namespace FSH.Modules.Catalog.Data;

/// <summary>
/// Ejecuta lecturas contra el catálogo del tenant `global` desde cualquier tenant,
/// sin tocar BaseDbContext: abre un scope hijo, fija el contexto multitenant a
/// `global` y resuelve un <see cref="CatalogDbContext"/> filtrado a ese tenant.
/// </summary>
public interface IGlobalCatalogReader
{
    Task<T> RunAsync<T>(Func<CatalogDbContext, CancellationToken, Task<T>> work, CancellationToken cancellationToken);
}

internal sealed class GlobalCatalogReader(
    IServiceScopeFactory scopeFactory,
    IMultiTenantStore<AppTenantInfo> tenantStore) : IGlobalCatalogReader
{
    public async Task<T> RunAsync<T>(Func<CatalogDbContext, CancellationToken, Task<T>> work, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(work);

        var globalTenant = await tenantStore.GetAsync(MultitenancyConstants.Global.Id).ConfigureAwait(false)
            ?? new AppTenantInfo(
                MultitenancyConstants.Global.Id, MultitenancyConstants.Global.Name,
                connectionString: string.Empty, MultitenancyConstants.Global.EmailAddress,
                issuer: MultitenancyConstants.Global.Issuer);

        using var scope = scopeFactory.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(globalTenant);

        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        return await work(db, cancellationToken).ConfigureAwait(false);
    }
}
