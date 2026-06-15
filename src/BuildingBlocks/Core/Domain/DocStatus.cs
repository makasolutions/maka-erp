namespace FSH.Framework.Core.Domain;

/// <summary>
/// Estado canónico de un documento <see cref="ISubmittable"/>.
/// Patrón inspirado en Frappe DocStatus, adaptado a .NET fuerte tipado y al dominio fiscal Colombia (ADR-0006).
/// </summary>
/// <remarks>
/// Transiciones permitidas:
/// <list type="bullet">
///   <item><see cref="Draft"/> → <see cref="Submitted"/></item>
///   <item><see cref="Submitted"/> → <see cref="Cancelled"/></item>
/// </list>
/// <see cref="Draft"/> se borra (no se cancela). <see cref="Cancelled"/> es terminal — para corregir un
/// documento cancelado se emite uno nuevo con <see cref="ISubmittable.AmendedFrom"/> apuntando al original.
/// </remarks>
#pragma warning disable CA1028 // Enum storage should be Int32 — usamos short porque se persiste como smallint (ADR-0006); CA1028 está pensado para enums in-memory, no para enums de almacenamiento.
public enum DocStatus : short
#pragma warning restore CA1028
{
    /// <summary>Editable y borrable. El documento aún no ha tenido efecto en otros módulos.</summary>
    Draft = 0,

    /// <summary>Firmado/aprobado/contabilizado. Inmutable. Disparó efectos contables/operativos.</summary>
    Submitted = 1,

    /// <summary>Cancelado tras estar Submitted. Terminal. Para corregir, emitir un nuevo documento con <see cref="ISubmittable.AmendedFrom"/>.</summary>
    Cancelled = 2,
}
