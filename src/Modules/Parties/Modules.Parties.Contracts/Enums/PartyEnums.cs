using System.Text.Json.Serialization;

namespace FSH.Modules.Parties.Contracts.Enums;

/// <summary>Roles que puede tener un tercero (cliente y/o proveedor). Flags.</summary>
[Flags]
[JsonConverter(typeof(JsonStringEnumConverter<PartyRole>))]
public enum PartyRole
{
    None     = 0,
    Customer = 1,
    Supplier = 2,
}

/// <summary>Naturaleza del tercero.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PartyKind>))]
public enum PartyKind
{
    Natural = 0,
    Juridica = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter<PartyStatus>))]
public enum PartyStatus
{
    Active = 0,
    Inactive = 1,
    Prospect = 2,
}

/// <summary>Etapa del ciclo comercial (CRM).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<LifecycleStage>))]
public enum LifecycleStage
{
    Lead = 0,
    Mql = 1,
    Sql = 2,
    Opportunity = 3,
    Customer = 4,
    Inactive = 5,
}

[JsonConverter(typeof(JsonStringEnumConverter<PartyTeamRole>))]
public enum PartyTeamRole
{
    Owner = 0,
    Member = 1,
}
