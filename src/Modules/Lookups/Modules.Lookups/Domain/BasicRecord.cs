using FSH.Framework.Core.Domain;

namespace FSH.Modules.Lookups.Domain;

/// <summary>
/// RegistroBásico — un valor de una <see cref="BasicTable"/> (ej. "NIT" en la tabla
/// "IdentificationType"). Los módulos consumidores guardan el <see cref="Code"/> (clave
/// estable), no un FK. <c>Value</c> es la etiqueta a mostrar.
///
/// <para>Es <see cref="IGlobalEntity"/>: hereda la semántica de su tabla — registros de
/// una tabla global llevan <c>TenantId == null</c>.</para>
/// </summary>
public sealed class BasicRecord : BaseEntity<Guid>, IGlobalEntity
{
    public Guid    BasicTableId { get; private set; }
    public string? TenantId     { get; private set; }            // null = global (espeja la tabla)
    public string  Code         { get; private set; } = default!; // idRegistro (ej. "NIT")
    public string  Value        { get; private set; } = default!; // Valor / etiqueta
    public int     SortOrder    { get; private set; }
    public bool    IsActive     { get; private set; }

    /// <summary>
    /// Código de sistema: NO se puede ELIMINAR (ni siquiera root). El <see cref="Code"/> ya es inmutable
    /// (<see cref="Update"/> no lo toca), así que la lógica que keya sobre el code (p. ej. ruteo DIAN sobre
    /// <c>ContactFunction.FACTURACION_ELECTRONICA</c>) queda a salvo. PR-2.
    /// </summary>
    public bool    IsProtected  { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    private BasicRecord() { }

    public static BasicRecord Create(
        Guid basicTableId,
        string code,
        string value,
        string? tenantId,
        int sortOrder = 0,
        bool isActive = true,
        bool isProtected = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new BasicRecord
        {
            Id           = Guid.CreateVersion7(),
            BasicTableId = basicTableId,
            Code         = code.Trim(),
            Value        = value.Trim(),
            TenantId     = tenantId,
            SortOrder    = sortOrder,
            IsActive     = isActive,
            IsProtected  = isProtected,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void Update(string value, int sortOrder, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value        = value.Trim();
        SortOrder    = sortOrder;
        IsActive     = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
