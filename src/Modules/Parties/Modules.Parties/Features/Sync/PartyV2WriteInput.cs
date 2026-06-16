using FSH.Modules.Parties.Contracts.Enums;
using FSH.Modules.Parties.Contracts.v1.Parties;

namespace FSH.Modules.Parties.Features.Sync;

/// <summary>
/// Entrada de escritura v2 en lenguaje v1 (PR-F1a). Lleva EXACTAMENTE los valores que el
/// <see cref="PartyV2Synchronizer"/> antes leía de las propiedades v1 del <c>Party</c>
/// (<c>Roles</c>, <c>CreditLimit</c>…). Desacopla el synchronizer de los campos v1 del agregado:
/// los handlers la construyen desde el comando y el synchronizer escribe SOLO v2 a partir de ella.
/// Cuando F1b quite los campos v1 del dominio, el synchronizer no se entera (ya no los leía).
/// </summary>
public sealed record PartyV2WriteInput(
    PartyRole Roles,
    decimal?  CreditLimit,
    string?   CreditCurrency,
    string?   CreditDaysCode,
    bool      CreditBlocked,
    string?   TaxRegimeCode,
    string?   ActividadEconomicaCiiuCode,
    // Ejes fiscales v2 autoritativos (≠ null → tienen precedencia sobre TaxRegimeCode). Front siempre los manda.
    PartyFiscalAxesInput? FiscalAxes = null);
