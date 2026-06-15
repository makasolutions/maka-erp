using FSH.Framework.Core.Domain;

namespace FSH.Modules.NamingSeries.Domain;

/// <summary>
/// Log inmutable de cada número emitido por una <see cref="NamingSeries"/>.
/// Una fila por asignación. Permite trazabilidad DIAN: qué documento consumió cada número,
/// cuándo y por quién.
/// </summary>
public sealed class NamingSeriesAllocation : BaseEntity<Guid>, IHasTenant
{
    public Guid NamingSeriesId { get; private set; }
    public string TenantId { get; private set; } = default!;
    public string Number { get; private set; } = default!;
    public string DocumentType { get; private set; } = default!;
    public Guid? DocumentId { get; private set; }
    public DateTimeOffset AllocatedAtUtc { get; private set; }
    public string AllocatedBy { get; private set; } = default!;

    private NamingSeriesAllocation() { }

    /// <summary>Crea el registro de asignación (siempre llamado por el allocator, nunca CRUD externo).</summary>
    public static NamingSeriesAllocation Record(
        NamingSeries series,
        string number,
        Guid? documentId,
        string allocatedBy,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(series);
        ArgumentException.ThrowIfNullOrWhiteSpace(number);
        ArgumentException.ThrowIfNullOrWhiteSpace(allocatedBy);
        return new NamingSeriesAllocation
        {
            Id = Guid.CreateVersion7(),
            NamingSeriesId = series.Id,
            TenantId = series.TenantId,
            Number = number,
            DocumentType = series.DocumentType,
            DocumentId = documentId,
            AllocatedAtUtc = now,
            AllocatedBy = allocatedBy,
        };
    }
}
