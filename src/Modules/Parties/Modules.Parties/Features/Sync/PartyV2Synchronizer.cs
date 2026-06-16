using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Core.Context;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;
using FSH.Modules.Parties.Data;
using FSH.Modules.Parties.Domain;
using FSH.Modules.Parties.Domain.Credit;
using FSH.Modules.Parties.Domain.Profiles;
using FSH.Modules.Parties.Migration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FSH.Modules.Parties.Features.Sync;

/// <summary>
/// Escritura v2 (PR-D5 dual-write → PR-F1a escritura PRIMARIA): escribe el estado v2 de UN tercero
/// a partir de un <see cref="PartyV2WriteInput"/> (valores en lenguaje v1 que el handler construye
/// desde el comando — ya NO lee las propiedades v1 del <c>Party</c>), dentro del mismo
/// <see cref="PartiesDbContext"/> que el handler → el v2 se persiste en el MISMO <c>SaveChanges</c>
/// (una sola transacción). Hasta F1b, el handler además sigue escribiendo las columnas v1 vía
/// <c>Party.Create/Update</c> (redundante, reversible); este componente nunca dependió de ellas.
///
/// Idempotente: diffea el estado v2 actual vs el deseado por el input. Re-guardar un tercero sin
/// cambios no duplica profiles ni genera movimientos de crédito espurios (el diff de cupo es contra
/// el <c>CupoAsignado</c> REAL del CreditAccount cargado, no contra un estado asumido).
/// </summary>
public sealed class PartyV2Synchronizer(
    PartiesDbContext db,
    ICurrentUser currentUser,
    IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
    ILogger<PartyV2Synchronizer> logger)
{
    /// <summary>Auditoría real de quién cambió el dato; sentinel cuando no hay usuario autenticado (ej. backfill/tests).</summary>
    private string RegistradoPor =>
        currentUser.IsAuthenticated() ? currentUser.GetUserId().ToString() : "dual-write";

    private string TenantId =>
        tenantAccessor.MultiTenantContext.TenantInfo?.Id
        ?? throw new InvalidOperationException("Tenant no resuelto para el dual-write.");

    /// <summary>
    /// Escribe el estado v2 de UN tercero a partir del <see cref="PartyV2WriteInput"/> (PR-F1a:
    /// la fuente ya NO son las propiedades v1 del <c>Party</c>, sino el input en lenguaje v1 que el
    /// handler construye desde el comando). En UPDATE, <paramref name="party"/> debe traer sus navs
    /// v2 cargadas (Include). Orden: facetas primero (la de cliente devuelve el <c>CustomerProfile</c>),
    /// luego crédito (usa esa referencia + el CreditAccount actual para diffear), luego el bloqueo.
    /// </summary>
    public async Task SyncAsync(Party party, PartyV2WriteInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(party);
        ArgumentNullException.ThrowIfNull(input);
        var registradoPor = RegistradoPor;

        var customerProfile = SyncCustomerFacet(party, input.Roles);
        SyncSupplierFacet(party, input.Roles);
        SyncEmployeeFacet(party, input.Roles);

        await SyncCreditAsync(party, customerProfile, input, registradoPor, ct).ConfigureAwait(false);
        await SyncCreditBlockedAsync(party, input, registradoPor, ct).ConfigureAwait(false);

        SyncFiscalData(party, input);
        SyncCiiu(party, input);
    }

    /// <summary>
    /// Sincroniza SOLO las facetas (Customer/Supplier/Employee) desde un set de roles, sin tocar
    /// crédito/fiscal/CIIU. Para <c>SetPartyRoles</c>, que únicamente cambia roles y no debe
    /// re-derivar el resto. <paramref name="party"/> debe traer sus navs de profile cargadas.
    /// </summary>
    public Task SyncFacetsAsync(Party party, PartyRole roles, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(party);
        SyncCustomerFacet(party, roles);
        SyncSupplierFacet(party, roles);
        SyncEmployeeFacet(party, roles);
        return Task.CompletedTask;
    }

    // ── FiscalData + CIIU (D5c) ────────────────────────────────────────────────────

    /// <summary>
    /// Escribe los ejes fiscales del FiscalData. DOS modos:
    /// (A) AUTORITATIVO — el comando trae <c>FiscalAxes</c> (front v2): escribe los ejes +
    ///     ResponsabilidadesFiscales tal cual (null limpia), con guarda anti-wipe (ver
    ///     <see cref="ApplyFiscalAxes"/>). Es el único camino que escribe la LISTA.
    /// (B) LEGACY — sin FiscalAxes: deriva los 2 ejes desde el <c>TaxRegimeCode</c> string (reusa
    ///     <see cref="TaxRegimeMapper"/>) con DOBLE CANDADO (record with + ?? current) — best-effort,
    ///     preserva lo no derivado. No mapeable → deja FiscalData como está + log.
    /// FiscalData es owned inline → la mutación la detecta el DetectChanges del handler.
    /// </summary>
    private void SyncFiscalData(Party party, PartyV2WriteInput input)
    {
        if (input.FiscalAxes is { } axes)
        {
            ApplyFiscalAxes(party, axes);
            return;
        }

        var mapping = TaxRegimeMapper.Map(input.TaxRegimeCode);
        if (mapping.IsEmpty) return; // v1 sin código → nada que derivar

        if (!mapping.Mapped)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("[dual-write] Party {PartyId}: TaxRegimeCode '{Code}' no mapea a régimen — FiscalData sin cambios.",
                    party.Id, input.TaxRegimeCode);
            }
            return;
        }

        var current = party.FiscalData ?? FiscalData.Empty;
        var newRegimen = mapping.Regimen ?? current.RegimenTributario;
        var newIva = mapping.ResponsabilidadIVA ?? current.ResponsabilidadIVA;

        if (newRegimen != current.RegimenTributario || newIva != current.ResponsabilidadIVA)
        {
            party.AssignFiscalData(current with { RegimenTributario = newRegimen, ResponsabilidadIVA = newIva });
        }
        // ejes sin cambio → no-op (idempotente)
    }

    /// <summary>
    /// Modo AUTORITATIVO: escribe los ejes + ResponsabilidadesFiscales tal cual vienen del comando
    /// (null/lista-vacía limpia — el front es la autoridad del estado fiscal). DOBLE CANDADO mantenido
    /// (record with preserva GranContribuyente/FlagPEP/FormaJuridica/etc.). Idempotente (solo asigna
    /// si algo cambió). GUARDA ANTI-WIPE: un FiscalAxes vacío-total (2 ejes null + lista vacía) sobre
    /// un FiscalData YA poblado NO lo borra — es casi seguro un no-op o un bug de lectura, no una
    /// intención real (un tercero siempre tiene naturaleza fiscal). Borrar en silencio se notaría
    /// recién en una auditoría DIAN.
    /// </summary>
    private static void ApplyFiscalAxes(Party party, PartyFiscalAxesInput axes)
    {
        IReadOnlyList<string> responsabilidades = axes.ResponsabilidadesFiscales ?? [];

        bool allEmpty = axes.RegimenTributario is null && axes.ResponsabilidadIVA is null && responsabilidades.Count == 0;
        if (allEmpty && party.FiscalData is not null) return; // guarda anti-wipe

        var current = party.FiscalData ?? FiscalData.Empty;
        bool changed =
            axes.RegimenTributario != current.RegimenTributario ||
            axes.ResponsabilidadIVA != current.ResponsabilidadIVA ||
            !responsabilidades.SequenceEqual(current.ResponsabilidadesFiscales);

        if (changed)
        {
            party.AssignFiscalData(current with
            {
                RegimenTributario = axes.RegimenTributario,
                ResponsabilidadIVA = axes.ResponsabilidadIVA,
                ResponsabilidadesFiscales = responsabilidades,
            });
        }
    }

    /// <summary>
    /// ActividadEconomicaCiiuCode (único v1) → la actividad PRINCIPAL en v2. El dual-write es dueño
    /// SOLO de la principal derivada de v1; las CIIU no-principales (secundarias deliberadas de v2,
    /// post-D) nunca se tocan. Al cambiar el código v1 se actualiza el código de la fila principal
    /// IN PLACE (sin tocar IsPrincipal) → sin swap del flag que viole ix_ciiu_principal. v1 sin código
    /// → no-op (no remueve). Requiere party.CiiuActivities cargado (Include en Update).
    /// </summary>
    private void SyncCiiu(Party party, PartyV2WriteInput input)
    {
        var desired = input.ActividadEconomicaCiiuCode?.Trim();
        if (string.IsNullOrWhiteSpace(desired)) return;

        var principal = party.CiiuActivities.FirstOrDefault(c => c.IsPrincipal);
        if (principal is null)
        {
            var activity = PartyCiiuActivity.Create(party.Id, desired, isPrincipal: true);
            db.PartyCiiuActivities.Add(activity); // Added explícito (sobrevive la danza del Update)
            party.CiiuActivities.Add(activity);   // grafo en memoria coherente
        }
        else if (!string.Equals(principal.CiiuCode, desired, StringComparison.Ordinal))
        {
            principal.ChangeCode(desired); // UPDATE in place, sin tocar IsPrincipal
        }
        // principal.CiiuCode == desired → no-op (idempotente)
    }

    // ── Facetas (D5a) ──────────────────────────────────────────────────────────────
    // Customer/Supplier/Employee desde los flags Roles v1 (Contact/Partner no tienen flag v1).
    // Crear / reactivar / desactivar SOFT (IsActive=false, preserva historial). Idempotente.

    /// <summary>Devuelve el CustomerProfile ACTIVO (creado o existente) si el rol Customer está; si no, null.</summary>
    private CustomerProfile? SyncCustomerFacet(Party party, PartyRole roles)
    {
        if (roles.HasFlag(PartyRole.Customer))
        {
            if (party.CustomerProfile is null)
            {
                var cp = CustomerProfile.Create(party.Id);
                db.CustomerProfiles.Add(cp);
                return cp;
            }
            party.CustomerProfile.Activate();
            return party.CustomerProfile;
        }
        if (party.CustomerProfile is { IsActive: true }) party.CustomerProfile.Deactivate();
        return null;
    }

    private void SyncSupplierFacet(Party party, PartyRole roles)
    {
        if (roles.HasFlag(PartyRole.Supplier))
        {
            if (party.SupplierProfile is null) db.SupplierProfiles.Add(SupplierProfile.Create(party.Id));
            else party.SupplierProfile.Activate();
        }
        else if (party.SupplierProfile is { IsActive: true }) party.SupplierProfile.Deactivate();
    }

    private void SyncEmployeeFacet(Party party, PartyRole roles)
    {
        if (roles.HasFlag(PartyRole.Employee))
        {
            if (party.EmployeeProfile is null) db.EmployeeProfiles.Add(EmployeeProfile.Create(party.Id));
            else party.EmployeeProfile.Activate();
        }
        else if (party.EmployeeProfile is { IsActive: true }) party.EmployeeProfile.Deactivate();
    }

    // ── Crédito (D5b) ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Sincroniza el CreditAccount desde <c>CreditLimit</c> v1. El diff de cupo es contra el
    /// <c>CupoAsignado</c> REAL del account cargado → cero movimientos espurios en un re-save.
    /// </summary>
    private async Task SyncCreditAsync(Party party, CustomerProfile? customerProfile, PartyV2WriteInput input, string registradoPor, CancellationToken ct)
    {
        decimal desired = input.CreditLimit ?? 0m;

        // Sin faceta cliente activa: no se inventa crédito (consistente con PR-C). Anomalía → log.
        if (customerProfile is null)
        {
            if (desired > 0 && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("[dual-write] Party {PartyId}: CreditLimit>0 sin faceta cliente activa — crédito omitido.", party.Id);
            }
            return;
        }

        // Se carga SIN Include(Movements): solo se necesita CupoAsignado para diffear; el movimiento
        // nuevo se agrega a la colección (Added) y los movimientos en BD no se tocan.
        var account = await db.CreditAccounts
            .FirstOrDefaultAsync(a => a.CustomerProfileId == customerProfile.Id, ct).ConfigureAwait(false);

        if (account is null)
        {
            if (desired > 0)
            {
                db.CreditAccounts.Add(CreditAccount.Open(
                    customerProfile.Id, TenantId, desired, registradoPor,
                    monedaId: string.IsNullOrWhiteSpace(input.CreditCurrency) ? "COP" : input.CreditCurrency!,
                    diasCredito: int.TryParse(input.CreditDaysCode, out var d) ? d : 0));
            }
            return; // desired == 0 → no inventar crédito
        }

        if (desired > account.CupoAsignado)
        {
            var mov = account.AddMovement(CreditMovementType.Aumento, desired - account.CupoAsignado, registradoPor,
                motivo: "Dual-write: aumento de cupo (CreditLimit v1).");
            TrackNewMovement(mov);
            if (!account.EstaActivo) account.Reactivate();
        }
        else if (desired < account.CupoAsignado)
        {
            var mov = account.AddMovement(CreditMovementType.Reduccion, account.CupoAsignado - desired, registradoPor,
                motivo: "Dual-write: reducción de cupo (CreditLimit v1).");
            TrackNewMovement(mov);
            // CreditLimit→0: el account queda inactivo pero con su log intacto (la historia reconstruye
            // el cupo en cualquier punto del tiempo: asignación inicial → reducción a 0).
            if (desired == 0) account.Deactivate();
        }
        // desired == account.CupoAsignado → CERO movimientos (idempotencia — el crux de D5b).
    }

    /// <summary>
    /// Fuerza el estado Added de un movimiento NUEVO agregado a un CreditAccount YA rastreado
    /// (caso UPDATE). EF no puede distinguir "nuevo" de "existente" por la clave Guid generada en
    /// cliente, así que al aparecer en la colección de un padre rastreado lo trataría como UPDATE
    /// (afecta 0 filas → DbUpdateConcurrencyException). Mismo motivo que el MarkChildrenAdded del
    /// UpdatePartyCommandHandler para los hijos v1. En CREATE no hace falta: el account se agrega con
    /// db.Add y la cascada marca su AsignacionInicial como Added.
    /// </summary>
    private void TrackNewMovement(CreditMovement movement) => db.Entry(movement).State = EntityState.Added;

    /// <summary>
    /// CreditBlocked v1 → PartyHold(Ventas). ASIMETRÍA DELIBERADA: se CREA el hold cuando
    /// CreditBlocked=true (idempotente: skip si ya hay hold Ventas activo), pero NO se auto-libera
    /// cuando pasa a false. Un hold es una acción operativa deliberada que el flag v1 podría no
    /// conocer (bloqueo manual); auto-quitarlo lo desharía silenciosamente. Errar hacia mantener el
    /// bloqueo. La liberación es siempre manual.
    /// </summary>
    private async Task SyncCreditBlockedAsync(Party party, PartyV2WriteInput input, string registradoPor, CancellationToken ct)
    {
        if (!input.CreditBlocked) return;

        bool hasActiveVentasHold = await db.PartyHolds
            .AnyAsync(h => h.PartyId == party.Id && h.HoldType == HoldType.Ventas && h.EstaActivo, ct)
            .ConfigureAwait(false);
        if (hasActiveVentasHold) return; // idempotente

        db.PartyHolds.Add(PartyHold.Place(
            party.Id, HoldType.Ventas,
            "Dual-write: CreditBlocked=true (crédito bloqueado en v1).",
            DateOnly.FromDateTime(DateTime.UtcNow), registradoPor));
    }
}
