using FSH.Framework.Core.Domain;

namespace FSH.Modules.Lookups.Domain;

/// <summary>
/// Departamento DIVIPOLA (DANE). Dato de referencia GLOBAL y compartido entre todos los
/// tenants — implementa <see cref="IGlobalEntity"/> para opt-out del filtro de tenant.
/// <c>Code</c> es el código DIVIPOLA de 2 dígitos (ej. "05" Antioquia, "11" Bogotá D.C.).
/// </summary>
public sealed class Department : BaseEntity<Guid>, IGlobalEntity
{
    public string Code { get; private set; } = default!; // DIVIPOLA 2 dígitos
    public string Name { get; private set; } = default!;

    public ICollection<Municipality> Municipalities { get; private set; } = new List<Municipality>();

    private Department() { }

    public static Department Create(string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Department { Id = Guid.CreateVersion7(), Code = code.Trim(), Name = name.Trim() };
    }
}
