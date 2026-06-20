# Deudas de deprecación rastreables

Registro de elementos marcados **deprecated** que NO se eliminaron en el momento (para no romper
contratos/migraciones) y deben limpiarse en un PR de mantenimiento futuro.

| Elemento | Dónde | Introducido | Razón de no borrar ya | Acción futura |
|---|---|---|---|---|
| `FiscalData.FlagPEP` + columna `parties.ContactProfiles`… `Parties.FiscalData_FlagPEP` | `src/Modules/Parties/.../Domain/FiscalData.cs`, `PartyConfiguration.cs`, `PartyV2Dto.cs` (`PartyV2FiscalDto.FlagPEP`) | PR-2 (2026-06-19) | El PEP pasó a ser atributo de **persona** (`Party.IsPEP`/`PepType`). No se dropeó la columna `FiscalData_FlagPEP` para no romper el VO owned ni forzar una migración de datos en el mismo PR. | Eliminar la columna `FiscalData_FlagPEP`, el campo `FlagPEP` del VO y del DTO en un PR de limpieza. La relación rep-legal expone `persona.IsPEP` (no lo duplica). |

> Cómo se consume: cada entrada apunta al `TODO(mantenimiento)` en el código. Al hacer el PR de limpieza,
> borrar la fila aquí y el TODO correspondiente.
