using System.Data.Common;

namespace FSH.Modules.NamingSeries.Contracts;

/// <summary>
/// Superficie pública del módulo para generar números atómicos de series tenant-aisladas (ADR-0007).
///
/// La operación se hace sobre la <see cref="DbConnection"/> y <see cref="DbTransaction"/> del
/// módulo cliente — NO sobre el <c>NamingSeriesDbContext</c> — para garantizar atomicidad
/// transaccional con el documento (factura, OC, asiento) que consume el número. Si la transacción
/// del cliente hace rollback, el incremento de <c>CurrentValue</c> se revierte automáticamente.
/// </summary>
public interface INamingSeriesAllocator
{
    /// <summary>
    /// Genera el siguiente número para <paramref name="documentType"/> en el tenant actual,
    /// dentro de la transacción provista por el caller. Hace SELECT … FOR UPDATE sobre la serie
    /// activa, valida vigencia/rango, incrementa el contador, registra el log de asignación y
    /// publica el integration event de umbral si corresponde.
    /// </summary>
    /// <param name="documentType">Discriminador del tipo de documento (ej. "SalesInvoice", "PurchaseOrder").</param>
    /// <param name="documentId">ID del documento que consumirá el número (para trazabilidad). <c>null</c> si aún no existe.</param>
    /// <param name="connection">Conexión Postgres abierta del DbContext del módulo cliente.</param>
    /// <param name="transaction">Transacción activa sobre <paramref name="connection"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Número formateado según el patrón de la serie activa.</returns>
    Task<string> AllocateAsync(
        string documentType,
        Guid? documentId,
        DbConnection connection,
        DbTransaction transaction,
        CancellationToken ct = default);
}
