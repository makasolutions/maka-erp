using Wolverine;

namespace FSH.Framework.Eventing.Tenant;

/// <summary>
/// Registro del middleware incoming de tenant (INV-9 estructural). Llamar dentro del
/// callback de <c>builder.UseWolverine(opts =&gt; ...)</c> en el Program.cs del host.
///
/// El outgoing rule está diferido — ver <c>TenantEnvelopeRule.cs</c> y
/// <c>docs/specs/platform/wolverine-phase1-followups.md</c> ("Outgoing rule INV-9").
/// </summary>
public static class TenantWolverineExtensions
{
    /// <summary>
    /// Activa <see cref="TenantContextMiddleware"/> sobre todos los handlers Wolverine
    /// del host. El middleware restaura el Finbuckle <c>ITenantInfo</c> desde
    /// <c>envelope.TenantId</c> ANTES de ejecutar el handler.
    /// </summary>
    public static WolverineOptions UseTenantContextMiddleware(this WolverineOptions opts)
    {
        ArgumentNullException.ThrowIfNull(opts);
        opts.Policies.AddMiddleware<TenantContextMiddleware>();
        return opts;
    }
}
