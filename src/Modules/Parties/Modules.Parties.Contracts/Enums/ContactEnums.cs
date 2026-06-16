using System.Text.Json.Serialization;

namespace FSH.Modules.Parties.Contracts.Enums;

/// <summary>Función de una persona de contacto B2B (SPEC §6.3 / §13).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContactFunction>))]
public enum ContactFunction : short
{
    FacturacionElectronica = 1,
    CuentasPorPagar = 2,
    Financiero = 3,
    Comercial = 4,
    TI = 5,
    Gerencia = 6,
    Otro = 99,
}
