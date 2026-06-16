using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.V2.Profiles;

namespace FSH.Modules.Parties.Features.Sync;

/// <summary>
/// Dual-write v1→v2 (PR-D5): sincroniza el estado v2 de UN tercero desde su estado v1, dentro del
/// mismo <see cref="PartiesDbContext"/> que el handler — así el v2 se persiste en el MISMO
/// <c>SaveChanges</c> (una sola transacción; nunca queda v1 escrito y v2 no).
///
/// Idempotente: diffea el estado v2 actual vs el deseado por v1. Re-guardar un tercero sin cambios
/// no duplica profiles ni muta estado (Activate/Deactivate son no-op si ya están en el estado pedido).
///
/// NOTA(unificación futura): el caso "crear profile" se solapa con el mapeo de
/// <see cref="Migration.PartiesV2BackfillService"/> (bulk, create-only, one-time). Se mantienen
/// separados a propósito (este es diff/dual-write por request). Si la duplicación se vuelve carga de
/// mantenimiento, unificar haciendo que el backfill delegue aquí por-party — buscar esta nota.
/// </summary>
public sealed class PartyV2Synchronizer(PartiesDbContext db)
{
    /// <summary>
    /// PR-D5a — sincroniza las facetas Customer/Supplier/Employee desde los flags <c>Roles</c> v1.
    /// Contact/Partner NO tienen flag v1 (se crean por features v2 dedicadas), no se tocan aquí.
    ///
    /// En UPDATE, <paramref name="party"/> debe traer sus navs v2 cargadas (Include) para diffear.
    /// En CREATE, las navs son null (tercero nuevo) → se crean los profiles que correspondan.
    /// </summary>
    public void SyncProfilesFromRoles(Party party)
    {
        ArgumentNullException.ThrowIfNull(party);

        // Customer
        if (party.IsCustomer)
        {
            if (party.CustomerProfile is null) db.CustomerProfiles.Add(CustomerProfile.Create(party.Id));
            else party.CustomerProfile.Activate(); // idempotente; reactiva si estaba soft-desactivado
        }
        else if (party.CustomerProfile is { IsActive: true })
        {
            party.CustomerProfile.Deactivate(); // soft (IsActive=false): preserva historial, no borra
        }

        // Supplier
        if (party.IsSupplier)
        {
            if (party.SupplierProfile is null) db.SupplierProfiles.Add(SupplierProfile.Create(party.Id));
            else party.SupplierProfile.Activate();
        }
        else if (party.SupplierProfile is { IsActive: true })
        {
            party.SupplierProfile.Deactivate();
        }

        // Employee (rol exclusivo según PartyMapping.NormalizeRoles)
        if (party.IsEmployee)
        {
            if (party.EmployeeProfile is null) db.EmployeeProfiles.Add(EmployeeProfile.Create(party.Id));
            else party.EmployeeProfile.Activate();
        }
        else if (party.EmployeeProfile is { IsActive: true })
        {
            party.EmployeeProfile.Deactivate();
        }
    }
}
