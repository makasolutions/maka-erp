using FSH.Framework.Core.Domain;

namespace FSH.Modules.SharedRecords.Domain;

/// <summary>
/// Dirección genérica polimórfica (control reutilizable). Se asocia a CUALQUIER entidad vía
/// <see cref="OwnerType"/> (string, sin FK dura cross-módulo) + <see cref="OwnerId"/>. No es
/// específica de Party. <see cref="LabelCode"/> = clasificación (Tabla Básica <c>AddressLabel</c>).
/// Invariante: a lo sumo UNA principal por (TenantId, OwnerType, OwnerId) — ver
/// <see cref="AddressPrimaryPolicy"/> + índice único parcial en la configuración EF.
/// </summary>
public sealed class Address : BaseEntity<Guid>, IAuditableEntity, ISoftDeletable
{
    public string  OwnerType { get; private set; } = default!;
    public Guid    OwnerId   { get; private set; }

    public string? LabelCode { get; private set; }      // Tabla Básica AddressLabel (Bodega/Sede/…)
    public bool    IsActive  { get; private set; }
    public bool    IsPrimary { get; private set; }

    public string  Country          { get; private set; } = "Colombia";
    public string? Department        { get; private set; } // nombre visible (compat)
    public string? City             { get; private set; } // nombre visible (compat)
    public string? DepartmentCode   { get; private set; } // DIVIPOLA 2 díg.
    public string? MunicipalityCode { get; private set; } // DIVIPOLA/DANE 5 díg.
    public string? Line             { get; private set; } // dirección cruda (NormalizedLine se difiere al cutover)
    public string? Barrio           { get; private set; }
    public string? Reference        { get; private set; }
    public decimal? Latitude        { get; private set; }
    public decimal? Longitude       { get; private set; }

    // IAuditableEntity
    public DateTimeOffset  CreatedOnUtc      { get; private set; }
    public string?         CreatedBy         { get; private set; }
    public DateTimeOffset? LastModifiedOnUtc { get; private set; }
    public string?         LastModifiedBy    { get; private set; }

    // ISoftDeletable
    public bool            IsDeleted    { get; private set; }
    public DateTimeOffset? DeletedOnUtc { get; private set; }
    public string?         DeletedBy    { get; private set; }

    private Address() { }

    public static Address Create(
        string ownerType, Guid ownerId, string? labelCode, bool isActive, bool isPrimary,
        string country, string? department, string? city, string? departmentCode, string? municipalityCode,
        string? line, string? barrio, string? reference, decimal? latitude, decimal? longitude)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerType);
        if (ownerId == Guid.Empty) throw new ArgumentException("OwnerId requerido.", nameof(ownerId));

        var address = new Address { Id = Guid.CreateVersion7(), OwnerType = ownerType.Trim(), OwnerId = ownerId };
        address.Apply(labelCode, isActive, isPrimary, country, department, city, departmentCode,
            municipalityCode, line, barrio, reference, latitude, longitude);
        return address;
    }

    /// <summary>Actualiza los campos editables. El owner es inmutable.</summary>
    public void Update(
        string? labelCode, bool isActive, bool isPrimary,
        string country, string? department, string? city, string? departmentCode, string? municipalityCode,
        string? line, string? barrio, string? reference, decimal? latitude, decimal? longitude) =>
        Apply(labelCode, isActive, isPrimary, country, department, city, departmentCode,
            municipalityCode, line, barrio, reference, latitude, longitude);

    /// <summary>Fija el flag principal (idempotente). La coordinación entre hermanas la hace
    /// <see cref="AddressPrimaryPolicy"/>.</summary>
    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;

    private void Apply(
        string? labelCode, bool isActive, bool isPrimary,
        string country, string? department, string? city, string? departmentCode, string? municipalityCode,
        string? line, string? barrio, string? reference, decimal? latitude, decimal? longitude)
    {
        LabelCode = string.IsNullOrWhiteSpace(labelCode) ? null : labelCode.Trim();
        IsActive = isActive;
        IsPrimary = isPrimary;
        Country = string.IsNullOrWhiteSpace(country) ? "Colombia" : country.Trim();
        Department = department?.Trim();
        City = city?.Trim();
        DepartmentCode = string.IsNullOrWhiteSpace(departmentCode) ? null : departmentCode.Trim();
        MunicipalityCode = string.IsNullOrWhiteSpace(municipalityCode) ? null : municipalityCode.Trim();
        Line = line?.Trim();
        Barrio = barrio?.Trim();
        Reference = reference?.Trim();
        Latitude = latitude;
        Longitude = longitude;
    }
}
