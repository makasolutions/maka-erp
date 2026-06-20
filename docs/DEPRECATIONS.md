# Deudas de deprecación rastreables

Registro de elementos marcados **deprecated** que NO se eliminaron en el momento (para no romper
contratos/migraciones) y deben limpiarse en un PR de mantenimiento futuro.

| Elemento | Dónde | Introducido | Razón de no borrar ya | Acción futura |
|---|---|---|---|---|
| `FiscalData.FlagPEP` + columna `parties.ContactProfiles`… `Parties.FiscalData_FlagPEP` | `src/Modules/Parties/.../Domain/FiscalData.cs`, `PartyConfiguration.cs`, `PartyV2Dto.cs` (`PartyV2FiscalDto.FlagPEP`) | PR-2 (2026-06-19) | El PEP pasó a ser atributo de **persona** (`Party.IsPEP`/`PepType`). No se dropeó la columna `FiscalData_FlagPEP` para no romper el VO owned ni forzar una migración de datos en el mismo PR. | Eliminar la columna `FiscalData_FlagPEP`, el campo `FlagPEP` del VO y del DTO en un PR de limpieza. La relación rep-legal expone `persona.IsPEP` (no lo duplica). |

> Cómo se consume: cada entrada apunta al `TODO(mantenimiento)` en el código. Al hacer el PR de limpieza,
> borrar la fila aquí y el TODO correspondiente.

---

# Hardening pendiente (validación / datos)

Defectos de robustez detectados que se mitigaron parcialmente y requieren un endurecimiento futuro
para que el problema no reaparezca por entrada de datos.

| Tema | Dónde | Detectado | Mitigación aplicada | Acción futura |
|---|---|---|---|---|
| **Editor de branding permite persistir una paleta dark con valores claros** (`DarkSurfaceColor=#FFFFFF`, `DarkBackgroundColor=#F8FAFC`) | UI de apariencia/branding (admin) → `PUT /api/v1/tenants/theme` (`UpdateTenantTheme*`). Validador actual: `UpdateTenantThemeCommandValidator.cs` | Bug theming Dark (2026-06-20) | (1) `TenantThemeDto.Default` corregido (`IsDefault=true` + `DarkPalette=PaletteDto.DefaultDark`) → el **fallback** para tenants sin record ya es dark-correcto. (2) Reset de datos de los 2 records existentes (`root`, `maka-solutions`) vía `ResetThemeAsync` → paleta dark real (`#111827`/`#0B1220`). | Validar en el **editor + backend** que la **paleta dark no acepte valores claros** (p. ej. luminancia de surface/background por debajo de un umbral, o que difieran de la light), o el bug de datos reaparecerá al guardar desde la UI. **No** es parte del fix de theming ni de PR-3. |
