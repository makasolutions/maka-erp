using FSH.Framework.Core.Domain;

namespace FSH.Modules.Lookups.Domain;

/// <summary>
/// TablaBásica — define una lista paramétrica administrable (tipos de identificación,
/// bancos, métodos de pago, canales de venta, etc.). Sus valores viven en <see cref="BasicRecord"/>.
///
/// <para><b>Multitenancy manual:</b> es <see cref="IGlobalEntity"/> (opt-out del filtro
/// automático de tenant). <c>TenantId == null</c> ⇒ tabla GLOBAL (compartida, solo
/// root/operador la edita); <c>TenantId == "&lt;tenant&gt;"</c> ⇒ tabla propia del tenant.
/// La lectura filtra <c>TenantId IS NULL OR TenantId == current</c> en los handlers.</para>
/// </summary>
public sealed class BasicTable : AggregateRoot<Guid>, ISoftDeletable, IGlobalEntity
{
    public string? TenantId      { get; private set; }            // null = global
    public string  Code          { get; private set; } = default!; // Identificador (ej. "IdentificationType")
    public string  Name          { get; private set; } = default!;
    public string? Description    { get; private set; }
    public bool    IsManageable   { get; private set; }            // esAdministrable
    public int     SortOrder      { get; private set; }            // Orden
    public bool    VisibleInMenu  { get; private set; }            // VisibleEnMenu
    public bool    IsGlobal       { get; private set; }            // esGlobal

    public DateTime  CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }

    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    public ICollection<BasicRecord> Records { get; private set; } = new List<BasicRecord>();

    private BasicTable() { }

    public static BasicTable Create(
        string code,
        string name,
        bool isGlobal,
        string? tenantId,
        string? description = null,
        bool isManageable = true,
        int sortOrder = 0,
        bool visibleInMenu = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new BasicTable
        {
            Id            = Guid.CreateVersion7(),
            Code          = code.Trim(),
            Name          = name.Trim(),
            Description   = description?.Trim(),
            IsGlobal      = isGlobal,
            TenantId      = isGlobal ? null : tenantId,
            IsManageable  = isManageable,
            SortOrder     = sortOrder,
            VisibleInMenu = visibleInMenu,
            CreatedAtUtc  = DateTime.UtcNow,
        };
    }

    public void Update(
        string name,
        string? description,
        bool isManageable,
        int sortOrder,
        bool visibleInMenu)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name          = name.Trim();
        Description    = description?.Trim();
        IsManageable   = isManageable;
        SortOrder      = sortOrder;
        VisibleInMenu  = visibleInMenu;
        UpdatedAtUtc   = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (!IsDeleted) return;
        IsDeleted    = false;
        DeletedOnUtc = null;
        DeletedBy    = null;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
