using System.Text.Json.Serialization;

namespace FSH.Modules.Parties.Contracts.Enums;

/// <summary>
/// Tipo de bloqueo de un tercero (patrón Frappe Block/Hold, SPEC §10 / §13).
/// <see cref="Todo"/> cubre todas las operaciones.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<HoldType>))]
public enum HoldType : short
{
    Ventas = 1,
    Compras = 2,
    Pagos = 3,
    Cobros = 4,
    Todo = 99,
}
