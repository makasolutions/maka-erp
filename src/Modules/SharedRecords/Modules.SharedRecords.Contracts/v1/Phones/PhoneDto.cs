namespace FSH.Modules.SharedRecords.Contracts.v1.Phones;

/// <summary>
/// Teléfono genérico polimórfico: se asocia a cualquier entidad vía <see cref="OwnerType"/> +
/// <see cref="OwnerId"/> (sin FK dura cross-módulo). <see cref="TypeCode"/> = clasificación
/// (Tabla Básica <c>PhoneType</c>: Fijo/Celular/Fax/WhatsApp). Una sola principal por owner.
/// </summary>
public sealed record PhoneDto(
    Guid    Id,
    string  OwnerType,
    Guid    OwnerId,
    string? TypeCode,
    bool    IsActive,
    bool    IsPrimary,
    string  Number,
    string? Extension,
    string? CountryCode);
