using FSH.Framework.Core.Domain;

namespace FSH.Modules.SharedRecords.Domain;

/// <summary>
/// Teléfono genérico polimórfico (control reutilizable). Se asocia a CUALQUIER entidad vía
/// <see cref="OwnerType"/> (string, sin FK dura cross-módulo) + <see cref="OwnerId"/>. No es
/// específico de Party. <see cref="TypeCode"/> = clasificación (Tabla Básica <c>PhoneType</c>:
/// Fijo/Celular/Fax/WhatsApp). Invariante: a lo sumo UNA principal por (TenantId, OwnerType,
/// OwnerId) — ver <see cref="PhonePrimaryPolicy"/> + índice único parcial en la configuración EF.
/// </summary>
public sealed class Phone : BaseEntity<Guid>, IAuditableEntity, ISoftDeletable
{
    public string  OwnerType { get; private set; } = default!;
    public Guid    OwnerId   { get; private set; }

    public string? TypeCode    { get; private set; }   // Tabla Básica PhoneType (Fijo/Celular/Fax/WhatsApp)
    public bool    IsActive    { get; private set; }
    public bool    IsPrimary   { get; private set; }
    public string  Number      { get; private set; } = default!;
    public string? Extension   { get; private set; }   // opcional
    public string? CountryCode { get; private set; }   // indicativo opcional (p.ej. "+57")

    // IAuditableEntity
    public DateTimeOffset  CreatedOnUtc      { get; private set; }
    public string?         CreatedBy         { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string?         LastModifiedBy    { get; private set; }

    // ISoftDeletable
    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    private Phone() { }

    public static Phone Create(
        string ownerType, Guid ownerId, string? typeCode, bool isActive, bool isPrimary,
        string number, string? extension, string? countryCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        if (ownerId == Guid.Empty) throw new ArgumentException("OwnerId requerido.", nameof(ownerId));

        var phone = new Phone { Id = Guid.CreateVersion7(), OwnerType = ownerType.Trim(), OwnerId = ownerId };
        phone.Apply(typeCode, isActive, isPrimary, number, extension, countryCode);
        return phone;
    }

    /// <summary>Actualiza los campos editables. El owner es inmutable.</summary>
    public void Update(string? typeCode, bool isActive, bool isPrimary, string number, string? extension, string? countryCode) =>
        Apply(typeCode, isActive, isPrimary, number, extension, countryCode);

    /// <summary>Fija el flag principal (idempotente). La coordinación entre hermanos la hace
    /// <see cref="PhonePrimaryPolicy"/>.</summary>
    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    private void Apply(string? typeCode, bool isActive, bool isPrimary, string number, string? extension, string? countryCode)
    {
        TypeCode = string.IsNullOrWhiteSpace(typeCode) ? null : typeCode.Trim();
        IsActive = isActive;
        IsPrimary = isPrimary;
        Number = number?.Trim() ?? string.Empty;
        Extension = string.IsNullOrWhiteSpace(extension) ? null : extension.Trim();
        CountryCode = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim();
    }
}
