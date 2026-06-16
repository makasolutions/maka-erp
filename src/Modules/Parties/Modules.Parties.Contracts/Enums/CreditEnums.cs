using System.Text.Json.Serialization;

namespace FSH.Modules.Parties.Contracts.Enums;

/// <summary>Tipo de movimiento en la cuenta de crédito de un cliente (SPEC §9 / §13).</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CreditMovementType>))]
public enum CreditMovementType : short
{
    AsignacionInicial = 1,
    Aumento = 2,
    Reduccion = 3,
    Consumo = 4,
    Liberacion = 5,
    Bloqueo = 6,
}
