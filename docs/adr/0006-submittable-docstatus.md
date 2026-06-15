# ADR-0006 · `ISubmittable` + `DocStatus` — patrón documento fiscal/transaccional

- **Estado:** Aceptado · junio 2026
- **Decisores:** Juan (CEO/sistemas) + asistente de planeación
- **Ligado a:** constitución §4 (doctrina *"nada se sobrescribe, todo se versiona"*), CLAUDE.md §8 (documentos aprobados/facturados son inmutables), CLAUDE.md §12 (señales de alerta — migraciones que mutan datos existentes), spec Catalog §141/§147 (convenios y scorecards inmutables en estado terminal).
- **Evidencia:** `src/BuildingBlocks/Core/Domain/{DocStatus,ISubmittable,SubmittableExtensions}.cs` · `src/Tests/Generic.Tests/Domain/SubmittableTests.cs`

---

## Contexto

Múltiples agregados del sistema modelan **documentos con efectos irreversibles**: una factura DIAN firmada, una OC aprobada, un asiento contabilizado, un convenio terminado, un scorecard cerrado. La regla de negocio es uniforme — **una vez firmados/aprobados, no se modifican; se cancelan, y para corregir se emite uno nuevo que apunta al cancelado** — pero hoy cada agregado rueda su propio enum `Status` con su propio guard ad-hoc (`Agreement.EnsureMutable()`, `Invoice.RequireStatus(Draft)`, `SupplierScorecard.Recompute()` con guardia en `Cerrado`, etc.). Esto:

1. **Duplica disciplina** — cada nueva entidad fiscal/transaccional re-inventa el patrón con riesgo de divergencia (un agregado olvida la guardia y permite mutar un documento firmado).
2. **Pierde linaje de correcciones** — la conexión documento-original ↔ documento-corregido no está modelada; el contador resuelve el rastro a mano.
3. **No es queryable de manera uniforme** — listar "documentos vigentes del tenant" requiere conocer el enum y el valor terminal de cada agregado.

Frappe Framework (Python, ERPNext) resuelve esto con `DocStatus { 0=Draft, 1=Submitted, 2=Cancelled }` aplicado a toda tabla submittable. La regla de Frappe es exactamente la que Maka necesita.

## Decisión

Definir un **building block transversal** en `BuildingBlocks/Core/Domain` que codifique el patrón:

1. **`DocStatus : short`** — enum canónico con valores `Draft = 0`, `Submitted = 1`, `Cancelled = 2`. Persistido como `smallint` (conversión EF explícita).
2. **`ISubmittable`** — interfaz que expone `DocStatus`, `SubmittedAt/By`, `CancelledAt/By`, `CancellationReason`, `AmendedFrom Guid?`. El interfaz NO impone métodos de transición: cada agregado define `Submit()`/`Cancel(reason, userId)` con la firma que su dominio requiere.
3. **`SubmittableExtensions`** — tres helpers de guardia: `EnsureMutable()` (lanza si no es `Draft`), `EnsureCanSubmit()` (lanza si no es `Draft`), `EnsureCanCancel()` (lanza si no es `Submitted`, con mensaje distinto según se intente cancelar un `Draft` o un `Cancelled`). Excepción: `InvalidOperationException` — consistente con `Agreement.EnsureMutable()` y `Invoice.RequireStatus()` ya existentes.
4. **Convención EF** documentada (en XML de la interfaz, no en código del BB):
   ```csharp
   builder.Property(x => x.DocStatus).HasConversion<short>();
   builder.HasIndex(x => new { x.TenantId, x.CreatedOnUtc })
          .HasFilter("\"DocStatus\" = 1")
          .HasDatabaseName("ix_{table}_active");
   builder.HasOne<TSelf>().WithMany()
          .HasForeignKey(x => x.AmendedFrom)
          .OnDelete(DeleteBehavior.NoAction);
   ```
   El índice parcial sobre `DocStatus = 1` cubre el 90% de las queries operativas (documentos vigentes); `AmendedFrom` es self-FK que preserva el linaje de correcciones.
5. **`IFiscalDocument` (campos DIAN específicos): se aplaza** hasta que llegue Facturación Electrónica DIAN (M8). Definir campos sin entidad concreta es premature design.

## Alcance del PR de creación

- Solo building block + tests + ADR.
- **Sin adopción de entidades existentes** en este PR — adopción se hace en PRs separados con su propio test de regresión.
- **Sin migración EF** — no hay entidades que adopten el interfaz todavía.

## Entidades NO adoptadas (justificación)

| Entidad | Razón |
|---|---|
| `Agreement` (Catalog) | 4 estados (Borrador · Vigente · Suspendido · Terminado). Encaje pobre con `DocStatus` (3 estados). Mantenerla en su enum extendido `AgreementStatus`. Documentar la divergencia. |
| `Invoice` (Billing SaaS) | Estado `Paid` no es transición de control (es estado de pago); `Void` ≈ `Cancelled` pero el agregado modela ciclo de facturación SaaS, no documento fiscal. Quedará intacto. |
| `Subscription` (Billing) | State machine de ciclo de vida (Active/Suspended/Cancelled). No es documento. |
| `PriceBulkProposal` | Workflow de aprobación (Pending/Approved/Rejected); al aprobar genera otro registro. Patrón distinto. |
| `PriceListItemHistory` | Append-only log inmutable de por sí. No necesita el patrón. |
| `Campaign` (PriceList) | Lifecycle programado (Scheduled/Running/Ended/Cancelled). No es documento. |
| `SupplierScorecard` (Catalog) | **Candidato 1:1 (Borrador → Cerrado).** Reservado para PR de adopción separado: requiere agregar el campo `AmendedFrom`, evaluar si introducir `Cancelled`, migrar el enum `ScorecardStatus` → `DocStatus`. Cambio cosmético + migración, no riesgoso, pero fuera del scope de este PR. |

## Entidades forward que adoptarán `ISubmittable`

| Módulo | Entidades |
|---|---|
| **DIAN (M8)** | `SalesInvoice`, `CreditNote`, `DebitNote`, `EquivalentDocument`, `PayrollDocument` — firmar = `Submitted` irreversible. |
| **Accounting (M7)** | `JournalEntry` (asiento contabilizado), `BankReconciliation`. |
| **Inventory (M3)** | `StockAdjustment`, `CycleCount`, `Transfer`. (`StockMovement` es ledger inmutable; probable que no necesite el wrapper.) |
| **Purchasing (M4)** | `PurchaseRequest`, `PurchaseOrder`, `GoodsReceipt`. |
| **Imports (M4)** | `ImportOrder` + liquidación. |
| **Orders (M2)** | `Quotation`, `SalesOrder`, `DeliveryNote`, `ReturnOrder`. |
| **Warranties (M10)** | `WarrantyCase`. |

## Validación upstream (FSH / Mukesh)

FullStackHero es **agnóstico al dominio fiscal** — no tiene un patrón equivalente. La decisión es **divergencia consciente**: tomamos como referente conceptual el `DocStatus` de Frappe Framework (Python, ERPNext) y lo adaptamos a:
- **.NET fuerte tipado** (enum `short` + helpers en lugar de columna mágica).
- **Dominio fiscal Colombia** (cancelación con razón obligatoria, linaje vía `AmendedFrom` requerido por DIAN para nota crédito/débito vinculada al original).
- **DDD táctico del proyecto** (lógica de transición en el agregado, no en un workflow externo).

No hay opción upstream que evaluar.

## Consecuencias

**Estructurales:**
- Nueva semántica transversal en `BuildingBlocks/Core/Domain` que codifica un invariante de negocio crítico (CLAUDE.md §8).
- Cada agregado submittable debe seguir la convención EF documentada en la interfaz (índice parcial + FK self-ref).

**Operativas:**
- Helpers compartidos eliminan el riesgo de divergencia entre agregados.
- Linaje `AmendedFrom` queryable de manera uniforme — base para reportes DIAN (notas crédito vinculadas al original).
- Sin impacto en código existente (no se adopta en ninguna entidad actual en este PR).

**De documentación:**
- Próxima entidad submittable: el handler que la cree debe implementar `Submit`/`Cancel` siguiendo el patrón de los tests (fixture `TestDoc` en `SubmittableTests`).

## Riesgos

1. 🟡 **Adopción inconsistente futura** — alguien crea una nueva entidad fiscal y rueda su propio enum. Mitigación: cuando exista la primera entidad real adoptante, agregar Architecture.Test que verifique "toda entidad con propiedad `DocStatus` implementa `ISubmittable` y llama a `EnsureCanSubmit/Cancel` en sus métodos".
2. 🟢 **`Agreement` queda con su enum propio** — divergencia consciente documentada. Riesgo bajo.
