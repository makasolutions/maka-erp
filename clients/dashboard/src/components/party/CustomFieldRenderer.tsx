import { Field, FormGrid, Combobox, type ComboboxOption } from "@/components/list";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { MakaDatePicker } from "@/components/maka/MakaDatePicker";
import { MakaCurrencyInput } from "@/components/maka/MakaCurrencyInput";
import { cn } from "@/lib/cn";
import type { CustomFieldDefinitionDto } from "@/api/custom-fields";

export type CustomFieldValues = Record<string, unknown>;

/** "minimal" = captura mínima (los requeridos NO bloquean). "complete" = al completar (sí se exigen). */
export type CustomFieldMode = "minimal" | "complete";

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

function isEmpty(v: unknown): boolean {
  if (v === null || v === undefined) return true;
  if (typeof v === "string") return v.trim().length === 0;
  if (Array.isArray(v)) return v.length === 0;
  return false;
}

/**
 * Validación de valores contra sus definiciones — espejo del backend `CustomFieldValues`.
 * Respeta la completitud gobernada: en modo "minimal" un requerido vacío NO es error (ajuste A);
 * solo lo es en modo "complete". Devuelve `{ slug → mensaje }`.
 */
export function validateCustomFieldValues(
  definitions: CustomFieldDefinitionDto[],
  values: CustomFieldValues,
  mode: CustomFieldMode,
  t: (key: string) => string,
): Record<string, string> {
  const errors: Record<string, string> = {};
  for (const def of definitions) {
    if (!def.activo) continue;
    const value = values[def.apiSlug];
    if (isEmpty(value)) {
      if (def.isRequired && mode === "complete") errors[def.apiSlug] = t("parties:validation.required");
      continue;
    }
    switch (def.fieldType) {
      case "Number":
      case "Currency":
        if (typeof value !== "number" || Number.isNaN(value)) errors[def.apiSlug] = t("parties:validation.number");
        break;
      case "EmailAddress":
        if (typeof value !== "string" || !EMAIL_RE.test(value)) errors[def.apiSlug] = t("parties:validation.email");
        break;
      case "Select":
        if (!def.options.some((o) => o.value === value)) errors[def.apiSlug] = t("parties:validation.option");
        break;
      case "MultiSelect":
        if (!Array.isArray(value) || value.some((v) => !def.options.some((o) => o.value === v)))
          errors[def.apiSlug] = t("parties:validation.option");
        break;
      default:
        break;
    }
  }
  return errors;
}

/**
 * Renderiza dinámicamente los custom fields de un `entityType` a partir de sus definiciones.
 * Reutilizable por la ficha del Tercero (Party) y, en PR-2, por `PartyRelationship`. No persiste:
 * sube cada cambio vía `onChange(slug, value)`; el caller decide cuándo validar/guardar.
 */
export function CustomFieldRenderer({
  definitions,
  values,
  onChange,
  mode = "minimal",
  errors,
  disabled,
}: {
  definitions: CustomFieldDefinitionDto[];
  values: CustomFieldValues;
  onChange: (slug: string, value: unknown) => void;
  mode?: CustomFieldMode;
  errors?: Record<string, string>;
  disabled?: boolean;
}) {
  const active = definitions.filter((d) => d.activo);
  if (active.length === 0) return null;

  return (
    <FormGrid>
      {active.map((def) => {
        const showRequired = def.isRequired && mode === "complete";
        const err = errors?.[def.apiSlug];
        const id = `cf-${def.apiSlug}`;
        const span = def.fieldType === "MultiSelect" ? 12 : 6;

        return (
          <Field
            key={def.id}
            id={id}
            span={span}
            label={def.title}
            required={showRequired}
            error={err}
            hint={def.description ?? undefined}
          >
            <CustomFieldControl id={id} def={def} value={values[def.apiSlug]} onChange={onChange} disabled={disabled} />
          </Field>
        );
      })}
    </FormGrid>
  );
}

function CustomFieldControl({
  id,
  def,
  value,
  onChange,
  disabled,
}: {
  id: string;
  def: CustomFieldDefinitionDto;
  value: unknown;
  onChange: (slug: string, value: unknown) => void;
  disabled?: boolean;
}) {
  const set = (v: unknown) => onChange(def.apiSlug, v);

  switch (def.fieldType) {
    case "Currency":
      return (
        <MakaCurrencyInput
          id={id}
          value={typeof value === "number" ? value : null}
          onChange={(v) => set(v)}
          disabled={disabled}
          ariaLabel={def.title}
        />
      );

    case "Number":
      return (
        <Input
          id={id}
          type="number"
          value={typeof value === "number" ? String(value) : ""}
          onChange={(e) => set(e.target.value === "" ? null : Number(e.target.value))}
          disabled={disabled}
        />
      );

    case "Date":
      return (
        <MakaDatePicker
          id={id}
          value={typeof value === "string" ? value : null}
          onChange={(iso) => set(iso)}
          disabled={disabled}
        />
      );

    case "Checkbox":
      return (
        <label className="flex h-9 items-center gap-2.5 text-[13px] font-medium text-[var(--color-foreground)]">
          <Switch checked={value === true} onCheckedChange={(c) => set(c)} disabled={disabled} aria-label={def.title} />
        </label>
      );

    case "Select": {
      const options: ComboboxOption[] = def.options.map((o) => ({ value: o.value, label: o.label }));
      return (
        <Combobox
          id={id}
          label={def.title}
          variant="field"
          value={typeof value === "string" ? value : null}
          onChange={(v) => set(v)}
          options={options}
          clearable
          disabled={disabled}
        />
      );
    }

    case "MultiSelect": {
      const selected = Array.isArray(value) ? (value as string[]) : [];
      const toggle = (optionValue: string) =>
        set(selected.includes(optionValue) ? selected.filter((v) => v !== optionValue) : [...selected, optionValue]);
      return (
        <div className="flex flex-wrap gap-2">
          {def.options.map((o) => {
            const on = selected.includes(o.value);
            return (
              <button
                key={o.value}
                type="button"
                disabled={disabled}
                onClick={() => toggle(o.value)}
                aria-pressed={on}
                className={cn(
                  "rounded-full border px-3 py-1 text-[12.5px] font-medium transition-colors",
                  on
                    ? "border-[var(--color-primary)] bg-[var(--color-primary)] text-[var(--color-primary-foreground)]"
                    : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-foreground)] hover:bg-[var(--color-muted)]",
                )}
              >
                {o.label}
              </button>
            );
          })}
        </div>
      );
    }

    case "EmailAddress":
      return (
        <Input
          id={id}
          type="email"
          value={typeof value === "string" ? value : ""}
          onChange={(e) => set(e.target.value)}
          disabled={disabled}
        />
      );

    case "PhoneNumber":
      return (
        <Input
          id={id}
          type="tel"
          value={typeof value === "string" ? value : ""}
          onChange={(e) => set(e.target.value)}
          disabled={disabled}
        />
      );

    case "Text":
    default:
      return (
        <Input
          id={id}
          value={typeof value === "string" ? value : ""}
          onChange={(e) => set(e.target.value)}
          disabled={disabled}
        />
      );
  }
}
