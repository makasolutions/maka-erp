namespace FSH.Framework.Core.Domain;

/// <summary>
/// Marca una entidad como documento con ciclo de vida controlado por <see cref="DocStatus"/>
/// (Borrador → Firmado → Cancelado). Patrón Frappe DocStatus adaptado a fiscal Colombia (ADR-0006).
/// </summary>
/// <remarks>
/// <para>
/// La lógica de transición (métodos <c>Submit()</c>/<c>Cancel(reason, userId)</c>) vive en cada agregado
/// concreto — el interfaz solo expone los campos. Los agregados deben llamar a los helpers de
/// <c>SubmittableExtensions</c> (<c>EnsureCanSubmit</c>, <c>EnsureCanCancel</c>, <c>EnsureMutable</c>)
/// para validar las precondiciones de cada transición.
/// </para>
/// <para>
/// <b>Convención EF para entidades <see cref="ISubmittable"/>:</b>
/// <code>
/// builder.Property(x =&gt; x.DocStatus).HasConversion&lt;short&gt;();
/// builder.HasIndex(x =&gt; new { x.TenantId, x.CreatedOnUtc })
///        .HasFilter("\"DocStatus\" = 1")
///        .HasDatabaseName("ix_{table}_active");
/// builder.HasOne&lt;TSelf&gt;().WithMany()
///        .HasForeignKey(x =&gt; x.AmendedFrom)
///        .OnDelete(DeleteBehavior.NoAction);
/// </code>
/// El índice parcial cubre el 90% de las queries operativas (documentos vigentes); cancelados/borradores
/// se consultan poco. <c>AmendedFrom</c> es self-FK que preserva el linaje de correcciones.
/// </para>
/// </remarks>
public interface ISubmittable
{
    /// <summary>Estado canónico del documento.</summary>
    DocStatus DocStatus { get; }

    /// <summary>Instante en que el documento pasó a <see cref="DocStatus.Submitted"/>.</summary>
    DateTimeOffset? SubmittedAt { get; }

    /// <summary>Identificador del usuario que firmó/aprobó el documento.</summary>
    string? SubmittedBy { get; }

    /// <summary>Instante en que el documento pasó a <see cref="DocStatus.Cancelled"/>.</summary>
    DateTimeOffset? CancelledAt { get; }

    /// <summary>Identificador del usuario que canceló el documento.</summary>
    string? CancelledBy { get; }

    /// <summary>Razón de cancelación. Obligatoria al cancelar.</summary>
    string? CancellationReason { get; }

    /// <summary>
    /// Cuando este documento es un <i>amend</i> (corrección) de uno cancelado, apunta al
    /// <c>Id</c> del documento original. <c>null</c> para emisiones originales.
    /// </summary>
    Guid? AmendedFrom { get; }
}
