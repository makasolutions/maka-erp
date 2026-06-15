namespace FSH.Framework.Core.Domain;

/// <summary>
/// Helpers de guardia para invariantes de transición de <see cref="ISubmittable"/> (ADR-0006).
/// Cada agregado los llama desde sus métodos <c>Submit()</c>, <c>Cancel(...)</c> y
/// <c>Update(...)</c> para garantizar que las precondiciones se aplican de manera uniforme
/// en cada agregado.
/// </summary>
public static class SubmittableExtensions
{
    /// <summary>
    /// Lanza si el documento NO está en <see cref="DocStatus.Draft"/>.
    /// Llamar desde <c>Update(...)</c> y cualquier mutación de contenido del documento.
    /// </summary>
    public static void EnsureMutable(this ISubmittable doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        if (doc.DocStatus != DocStatus.Draft)
        {
            throw new InvalidOperationException(
                $"El documento no es modificable (estado actual: {doc.DocStatus}). Solo documentos en Draft pueden editarse.");
        }
    }

    /// <summary>
    /// Lanza si el documento NO puede transitar a <see cref="DocStatus.Submitted"/>.
    /// Llamar desde <c>Submit()</c> antes de cambiar el estado y estampar timestamps.
    /// </summary>
    public static void EnsureCanSubmit(this ISubmittable doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        if (doc.DocStatus != DocStatus.Draft)
        {
            throw new InvalidOperationException(
                $"No se puede firmar/aprobar el documento desde el estado {doc.DocStatus}. Solo Draft → Submitted está permitido.");
        }
    }

    /// <summary>
    /// Lanza si el documento NO puede transitar a <see cref="DocStatus.Cancelled"/>.
    /// Llamar desde <c>Cancel(reason, userId)</c> antes de cambiar el estado.
    /// Cancelled es terminal; Draft se borra (no se cancela).
    /// </summary>
    public static void EnsureCanCancel(this ISubmittable doc)
    {
        ArgumentNullException.ThrowIfNull(doc);
        if (doc.DocStatus != DocStatus.Submitted)
        {
            throw new InvalidOperationException(
                doc.DocStatus == DocStatus.Draft
                    ? "Un documento en Draft no se cancela: se borra."
                    : "Un documento Cancelado es terminal: para corregirlo, emitir uno nuevo con AmendedFrom apuntando al original.");
        }
    }
}
