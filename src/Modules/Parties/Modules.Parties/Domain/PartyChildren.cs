using FSH.Framework.Core.Domain;
using FSH.Modules.Parties.Contracts.Enums;

namespace FSH.Modules.Parties.Domain;

/// <summary>Dirección de un tercero (puede tener varias). Etiqueta = Tabla Básica.</summary>
public sealed class PartyAddress : BaseEntity<Guid>
{
    public Guid     PartyId   { get; private set; }
    public string?  LabelCode { get; private set; }   // Tabla Básica AddressLabel (Casa, Oficina…)
    public string   Country   { get; private set; } = "Colombia";
    public string?  Department { get; private set; }
    public string?  City      { get; private set; }
    public string?  Line      { get; private set; }
    public string?  Barrio    { get; private set; }
    public string?  Reference { get; private set; }
    public decimal? Latitude  { get; private set; }
    public decimal? Longitude { get; private set; }
    public bool     IsPrimary { get; private set; }

    private PartyAddress() { }

    public static PartyAddress Create(string country, string? department, string? city, string? line,
        string? barrio, string? reference, decimal? latitude, decimal? longitude, bool isPrimary, string? labelCode) => new()
    {
        Id = Guid.CreateVersion7(),
        Country = string.IsNullOrWhiteSpace(country) ? "Colombia" : country.Trim(),
        Department = department?.Trim(),
        City = city?.Trim(),
        Line = line?.Trim(),
        Barrio = barrio?.Trim(),
        Reference = reference?.Trim(),
        Latitude = latitude,
        Longitude = longitude,
        IsPrimary = isPrimary,
        LabelCode = labelCode?.Trim(),
    };
}

/// <summary>Persona de contacto dentro del tercero (cuentas B2B con varios contactos).</summary>
public sealed class PartyContact : BaseEntity<Guid>
{
    public Guid    PartyId      { get; private set; }
    public string  Reference    { get; private set; } = default!; // cargo/profesión libre (compat)
    public string? ContactTypeCode { get; private set; }
    public string? AreaCode        { get; private set; }
    public string? IdentificationTypeCode { get; private set; }
    public string? IdentificationNumber   { get; private set; }
    public string? FirstName    { get; private set; }
    public string? LastName     { get; private set; }
    public string? FullName     { get; private set; }             // derivado (compat/listado)
    public string? PositionCode { get; private set; }             // cargo (Tabla Básica)
    public string? ProfessionCode { get; private set; }
    public DateOnly? BirthDate  { get; private set; }
    public string? GenderCode   { get; private set; }
    public string? MaritalStatusCode { get; private set; }
    public string? Email        { get; private set; }             // requerido en captura
    public string? Phone        { get; private set; }
    public string? Cell         { get; private set; }             // requerido en captura
    public bool    IsCommercial { get; private set; }
    public string? Notes        { get; private set; }

    private PartyContact() { }

    public static PartyContact Create(
        string reference, string? contactTypeCode, string? areaCode, string? identificationTypeCode,
        string? identificationNumber, string? firstName, string? lastName, string? positionCode,
        string? professionCode, DateOnly? birthDate, string? genderCode, string? maritalStatusCode,
        string? email, string? phone, string? cell, bool isCommercial, string? notes) => new()
    {
        Id = Guid.CreateVersion7(),
        Reference = reference.Trim(),
        ContactTypeCode = contactTypeCode?.Trim(),
        AreaCode = areaCode?.Trim(),
        IdentificationTypeCode = identificationTypeCode?.Trim(),
        IdentificationNumber = identificationNumber?.Trim(),
        FirstName = firstName?.Trim(),
        LastName = lastName?.Trim(),
        FullName = $"{firstName?.Trim()} {lastName?.Trim()}".Trim(),
        PositionCode = positionCode?.Trim(),
        ProfessionCode = professionCode?.Trim(),
        BirthDate = birthDate,
        GenderCode = genderCode?.Trim(),
        MaritalStatusCode = maritalStatusCode?.Trim(),
        Email = email?.Trim(),
        Phone = phone?.Trim(),
        Cell = cell?.Trim(),
        IsCommercial = isCommercial,
        Notes = notes?.Trim(),
    };
}

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
