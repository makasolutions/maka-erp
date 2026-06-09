using FSH.Framework.Core.Domain;

namespace FSH.Modules.Lookups.Domain;

/// <summary>
/// Municipio DIVIPOLA (DANE). Dato de referencia GLOBAL (<see cref="IGlobalEntity"/>).
/// <c>Code</c> es el código DIVIPOLA de 5 dígitos = 2 (departamento) + 3 (municipio),
/// ej. "05001" Medellín. <c>DepartmentCode</c> es el código de su departamento.
/// </summary>
public sealed class Municipality : BaseEntity<Guid>, IGlobalEntity
{
    public string Code           { get; private set; } = default!; // DIVIPOLA 5 dígitos
    public string Name           { get; private set; } = default!;
    public string DepartmentCode { get; private set; } = default!; // FK lógica a Department.Code

    private Municipality() { }

    public static Municipality Create(string code, string name, string departmentCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentCode);
        return new Municipality
        {
            Id = Guid.CreateVersion7(),
            Code = code.Trim(),
            Name = name.Trim(),
            DepartmentCode = departmentCode.Trim(),
        };
    }
}
