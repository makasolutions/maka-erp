using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain;

/// <summary>Dirección de un tercero (puede tener varias). Etiqueta = Tabla Básica.</summary>
public sealed class PartyAddress : BaseEntity<Guid>
{
    public Guid     PartyId   { get; private set; }
    public string?  LabelCode { get; private set; }   // Tabla Básica AddressLabel (Casa, Oficina…)
    public string   Country   { get; private set; } = "Colombia";
    public string?  Department { get; private set; }  // nombre visible (compat)
    public string?  City      { get; private set; }   // nombre visible (compat)
    public string?  DepartmentCode  { get; private set; } // DIVIPOLA 2 díg.
    public string?  MunicipalityCode { get; private set; } // DIVIPOLA 5 díg.
    public string?  Line      { get; private set; }   // dirección cruda tal cual la escribió el cliente
    public string?  NormalizedLine { get; private set; }  // forma codificada DIAN (derivada)
    public DateTime? NormalizedAtUtc { get; private set; }
    public string?  Barrio    { get; private set; }
    public string?  Reference { get; private set; }
    public decimal? Latitude  { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool     IsPrimary { get; private set; }

    private PartyAddress() { }

    public static PartyAddress Create(string country, string? department, string? city, string? line,
        string? barrio, string? reference, decimal? latitude, decimal? longitude, bool isPrimary, string? labelCode,
        string? departmentCode = null, string? municipalityCode = null)
    {
        string? normalized = DianAddressNormalizer.Normalize(line);
        return new()
        {
            Id = Guid.CreateVersion7(),
            Country = string.IsNullOrWhiteSpace(country) ? "Colombia" : country.Trim(),
            Department = department?.Trim(),
            City = city?.Trim(),
            DepartmentCode = string.IsNullOrWhiteSpace(departmentCode) ? null : departmentCode.Trim(),
            MunicipalityCode = string.IsNullOrWhiteSpace(municipalityCode) ? null : municipalityCode.Trim(),
            Line = line?.Trim(),
            NormalizedLine = normalized,
            NormalizedAtUtc = normalized is null ? null : DateTime.UtcNow,
            Barrio = barrio?.Trim(),
            Reference = reference?.Trim(),
            Latitude = latitude,
            Longitude = longitude,
            IsPrimary = isPrimary,
            LabelCode = labelCode?.Trim(),
        };
    }
}

// PR-2: `PartyContact` v1 (lista denormalizada de contactos) ELIMINADA. Los contactos persona↔empresa
// viven ahora en `PartyRelationship` (M2M). Dev-limpio: borrado sin migración de datos.

/// <summary>Canal de contacto tipado (teléfono, WhatsApp, redes…). ChannelTypeCode = Tabla Básica.</summary>
public sealed class PartyChannel : BaseEntity<Guid>
{
    public Guid    PartyId         { get; private set; }
    public string  ChannelTypeCode { get; private set; } = default!;
    public string  Value           { get; private set; } = default!;
    public string? Reference       { get; private set; }
    public bool    IsPrimary       { get; private set; }

    private PartyChannel() { }

    public static PartyChannel Create(string channelTypeCode, string value, string? reference, bool isPrimary) => new()
    {
        Id = Guid.CreateVersion7(),
        ChannelTypeCode = channelTypeCode.Trim(),
        Value = value.Trim(),
        Reference = reference?.Trim(),
        IsPrimary = isPrimary,
    };
}

/// <summary>Equipo asignado al tercero (responsable + miembros).</summary>
public sealed class PartyTeamMember : BaseEntity<Guid>
{
    public Guid          PartyId { get; private set; }
    public Guid          UserId  { get; private set; }
    public PartyTeamRole Role    { get; private set; }

    private PartyTeamMember() { }

    public static PartyTeamMember Create(Guid userId, PartyTeamRole role) => new()
    {
        Id = Guid.CreateVersion7(),
        UserId = userId,
        Role = role,
    };
}
