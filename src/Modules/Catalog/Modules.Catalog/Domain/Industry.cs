using FSH.Framework.Core.Domain;

namespace FSH.Modules.Catalog.Domain;

/// <summary>
/// Industria / sector vertical (Moda, Electrónica, Hogar…) — vive en el tenant
/// `global`. Cada industria habilita uno o más subárboles de la taxonomía Google
/// (vía <see cref="IndustryCategory"/>) para filtrar las categorías visibles a un
/// tenant/proveedor de esa vertical.
/// </summary>
public sealed class Industry : AggregateRoot<Guid>
{
    public string Code      { get; private set; } = default!;
    public string Name      { get; private set; } = default!;
    public int    SortOrder { get; private set; }
    public bool   IsActive  { get; private set; }

    public DateTime  CreatedAtUtc { get; private set; }

    public ICollection<IndustryCategory> Roots { get; private set; } = new List<IndustryCategory>();

    private Industry() { }

    public static Industry Create(string code, string name, int sortOrder = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Industry
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            SortOrder = sortOrder,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
    }

    public void AddRoot(int rootGoogleCategoryId) =>
        Roots.Add(IndustryCategory.Create(Id, rootGoogleCategoryId));
}

/// <summary>
/// Industria seleccionada por un tenant consumidor (datos privados por-tenant).
/// Filtra las categorías globales visibles a ese tenant. Referencia al
/// <see cref="Industry"/> del tenant `global` por Id.
/// </summary>
public sealed class TenantIndustry : BaseEntity<Guid>
{
    public Guid IndustryId { get; private set; }

    private TenantIndustry() { }

    public static TenantIndustry Create(Guid industryId) => new()
    {
        Id = Guid.CreateVersion7(),
        IndustryId = industryId,
    };
}

/// <summary>Mapeo Industria → categoría raíz de Google (subárbol habilitado).</summary>
public sealed class IndustryCategory : BaseEntity<Guid>
{
    public Guid IndustryId           { get; private set; }
    public int  RootGoogleCategoryId { get; private set; }

    private IndustryCategory() { }

    public static IndustryCategory Create(Guid industryId, int rootGoogleCategoryId) => new()
    {
        Id = Guid.CreateVersion7(),
        IndustryId = industryId,
        RootGoogleCategoryId = rootGoogleCategoryId,
    };
}
