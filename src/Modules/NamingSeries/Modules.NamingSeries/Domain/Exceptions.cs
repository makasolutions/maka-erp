namespace FSH.Modules.NamingSeries.Domain;

#pragma warning disable CA1032 // Constructores estándar de Exception innecesarios — son excepciones de dominio con causa única.
#pragma warning disable RCS1194 // ídem.

public sealed class InvalidPatternException(string message) : InvalidOperationException(message);

public sealed class NamingSeriesExhaustedException(string seriesId, int currentValue, int upperBound)
    : InvalidOperationException($"La serie {seriesId} agotó el rango: CurrentValue={currentValue}, To={upperBound}. Renovar resolución antes de continuar.")
{
    public string SeriesId { get; } = seriesId;
    public int CurrentValue { get; } = currentValue;
    public int UpperBound { get; } = upperBound;
}

public sealed class NamingSeriesNotYetValidException(string seriesId, DateTimeOffset validFrom)
    : InvalidOperationException($"La serie {seriesId} no es vigente aún (ValidFrom={validFrom:O}).")
{
    public string SeriesId { get; } = seriesId;
    public DateTimeOffset ValidFrom { get; } = validFrom;
}

public sealed class NamingSeriesExpiredException(string seriesId, DateTimeOffset validUntil)
    : InvalidOperationException($"La serie {seriesId} venció el {validUntil:O}. Crear nueva serie/resolución para continuar.")
{
    public string SeriesId { get; } = seriesId;
    public DateTimeOffset ValidUntil { get; } = validUntil;
}

public sealed class MultipleActiveNamingSeriesException(string tenantId, string documentType, int count)
    : InvalidOperationException($"Existen {count} series activas para tenant '{tenantId}', documentType '{documentType}'. DIAN exige una sola — cerrar las antiguas antes de continuar.")
{
    public string TenantId { get; } = tenantId;
    public string DocumentType { get; } = documentType;
    public int Count { get; } = count;
}

public sealed class NamingSeriesClosedException(string seriesId)
    : InvalidOperationException($"La serie {seriesId} está cerrada y no acepta más operaciones.")
{
    public string SeriesId { get; } = seriesId;
}

#pragma warning restore RCS1194
#pragma warning restore CA1032
