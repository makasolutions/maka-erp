using System.Net;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Exceptions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Lookups.Data;
using FSH.Modules.Lookups.Domain;
using Microsoft.EntityFrameworkCore;

namespace FSH.Modules.Lookups.Features;

/// <summary>Helpers de tenant/autorización para tablas básicas (global = solo root).</summary>
internal static class LookupGuards
{
    internal static (string? TenantId, bool IsRoot) Tenant(IMultiTenantContextAccessor<AppTenantInfo> accessor)
    {
        string? id = accessor.MultiTenantContext?.TenantInfo?.Id;
        return (id, id == MultitenancyConstants.Root.Id);
    }

    /// <summary>Carga una tabla visible para el tenant y exige permiso de edición (global ⇒ root).</summary>
    internal static async Task<BasicTable> LoadEditableAsync(
        LookupsDbContext db, Guid id, string? tenantId, bool isRoot, CancellationToken ct)
    {
        var table = await db.BasicTables
            .Include(x => x.Records)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct)
            .ConfigureAwait(false);

        bool visible = table is not null && (table.TenantId is null || table.TenantId == tenantId);
        if (table is null || !visible)
            throw new CustomException("Tabla básica no encontrada.", Enumerable.Empty<string>(), HttpStatusCode.NotFound);

        if (table.TenantId is null && !isRoot)
            throw new CustomException("Solo el operador puede editar tablas globales.", Enumerable.Empty<string>(), HttpStatusCode.Forbidden);

        return table;
    }
}
