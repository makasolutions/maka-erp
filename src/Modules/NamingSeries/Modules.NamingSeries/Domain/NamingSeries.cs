using FSH.Framework.Core.Domain;

namespace FSH.Modules.NamingSeries.Domain;

/// <summary>
/// Agregado raíz de una serie de numeración tenant-aislada (ADR-0007).
/// Genera números consecutivos para un <see cref="DocumentType"/> dentro del rango [<see cref="From"/>, <see cref="To"/>]
/// y respetando la vigencia (<see cref="ValidFrom"/>/<see cref="ValidUntil"/>).
///
/// Para documentos fiscales: campos <see cref="ResolutionNumber"/>, <see cref="ResolutionDate"/>,
/// <see cref="ResolutionTechnicalKey"/> persisten los datos de la resolución DIAN.
///
/// El método <see cref="Allocate"/> valida e incrementa, pero NO persiste — el allocator del
/// módulo lo invoca dentro de una transacción que comparte conexión con el documento cliente.
/// </summary>
public sealed class NamingSeries : AggregateRoot<Guid>, IHasTenant, IAuditableEntity
{
    public string TenantId { get; private set; } = default!;
    public string DocumentType { get; private set; } = default!;

    public NamingPattern Pattern { get; private set; } = default!;
    public int From { get; private set; }
    public int To { get; private set; }
    public int CurrentValue { get; private set; }

    public DateTimeOffset? ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }

    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public string? ClosedBy { get; private set; }

    public bool Notified80Pct { get; private set; }

    // DIAN (opcionales — obligatorios para DocumentType fiscal, validado a nivel de aplicación).
    public string? ResolutionNumber { get; private set; }
    public DateTimeOffset? ResolutionDate { get; private set; }
    public string? ResolutionTechnicalKey { get; private set; }

    // IAuditableEntity
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    private NamingSeries() { }

    /// <summary>Capacidad total del rango. <c>To - From + 1</c>.</summary>
    public int Capacity => To - From + 1;

    /// <summary>True si la serie aún acepta operaciones (no cerrada).</summary>
    public bool IsOpen => ClosedAtUtc is null;

    public static NamingSeries Create(
        string tenantId,
        string documentType,
        NamingPattern pattern,
        int from,
        int to,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil,
        string? resolutionNumber,
        DateTimeOffset? resolutionDate,
        string? resolutionTechnicalKey,
        string createdBy,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentType);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdBy);
        if (from < 1) throw new ArgumentOutOfRangeException(nameof(from), "From debe ser ≥ 1.");
        if (to < from) throw new ArgumentOutOfRangeException(nameof(to), "To debe ser ≥ From.");
        if (validFrom is not null && validUntil is not null && validUntil < validFrom)
        {
            throw new ArgumentOutOfRangeException(nameof(validUntil), "ValidUntil debe ser ≥ ValidFrom.");
        }

        return new NamingSeries
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantId,
            DocumentType = documentType,
            Pattern = pattern,
            From = from,
            To = to,
            CurrentValue = from - 1,
            ValidFrom = validFrom,
            ValidUntil = validUntil,
            ResolutionNumber = resolutionNumber,
            ResolutionDate = resolutionDate,
            ResolutionTechnicalKey = resolutionTechnicalKey,
            CreatedOnUtc = now,
            CreatedBy = createdBy,
        };
    }

    /// <summary>
    /// Valida vigencia/rango/estado y reserva el siguiente número. Retorna el número formateado.
    /// NO persiste — el caller debe hacer UPDATE de <c>CurrentValue</c>/<c>Notified80Pct</c> e INSERT del log
    /// dentro de la misma transacción en la que invocó este método.
    /// </summary>
    public string Allocate(DateTimeOffset now)
    {
        if (!IsOpen)
        {
            throw new NamingSeriesClosedException(Id.ToString());
        }
        if (ValidFrom is not null && now < ValidFrom)
        {
            throw new NamingSeriesNotYetValidException(Id.ToString(), ValidFrom.Value);
        }
        if (ValidUntil is not null && now > ValidUntil)
        {
            throw new NamingSeriesExpiredException(Id.ToString(), ValidUntil.Value);
        }
        int next = CurrentValue + 1;
        if (next > To)
        {
            throw new NamingSeriesExhaustedException(Id.ToString(), CurrentValue, To);
        }

        CurrentValue = next;
        LastModifiedOnUtc = now;

        // Threshold 80% — idempotente (un solo evento por umbral).
        int usedFromRange = CurrentValue - From + 1;
        int threshold80 = (int)Math.Ceiling(0.8 * Capacity);
        if (!Notified80Pct && usedFromRange >= threshold80)
        {
            Notified80Pct = true;
            AddDomainEvent(new NamingSeriesThresholdReachedDomainEvent(
                EventId: Guid.NewGuid(),
                OccurredOnUtc: now,
                NamingSeriesId: Id,
                TenantIdValue: TenantId,
                DocumentType: DocumentType,
                CurrentValue: CurrentValue,
                Capacity: Capacity));
        }

        return Pattern.Format(CurrentValue, now);
    }

    public void Close(string closedBy, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(closedBy);
        if (!IsOpen)
        {
            throw new NamingSeriesClosedException(Id.ToString());
        }
        ClosedAtUtc = now;
        ClosedBy = closedBy;
        LastModifiedOnUtc = now;
        LastModifiedBy = closedBy;
    }
}
