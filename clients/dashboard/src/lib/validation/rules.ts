import type { TFunction } from "i18next";
import {
  isBlank, isEmail, isPhone, isUrl, isPersonName, isColombianAddress, isSlug, isSku,
} from "./predicates";

/**
 * Primitivas de validación componibles **por tipo de dato** (§17). Cada `Rule` recibe el
 * valor + `t` y devuelve un mensaje (clave i18n `common:validation.*`) o null. Se combinan
 * por campo con `check(value, t, ...rules)` y se corren sobre un objeto con
 * `validateSchema(value, schema, t)`. Espejo de FormValidationRules.cs (backend = verdad).
 *
 * Uso:
 *   const errs = validateSchema(form, {
 *     name: [rules.required(), rules.max(150)],
 *     email: [rules.email()],
 *     price: [rules.required(), rules.number({ min: 0 })],
 *   }, t);
 */
export type Rule = (value: unknown, t: TFunction) => string | null;

const str = (v: unknown): string => (v == null ? "" : String(v)).trim();
const num = (v: unknown): number | null => {
  if (isBlank(v)) return null;
  const n = typeof v === "number" ? v : Number(String(v).replace(/\s/g, ""));
  return Number.isNaN(n) ? NaN : n;
};

export const rules = {
  required: (msg = "validation.required"): Rule => (v, t) => (isBlank(v) ? t(msg) : null),

  min: (n: number): Rule => (v, t) => (!isBlank(v) && str(v).length < n ? t("validation.minLength", { n }) : null),
  max: (n: number): Rule => (v, t) => (!isBlank(v) && str(v).length > n ? t("validation.maxLength", { n }) : null),

  pattern: (re: RegExp, key = "validation.formatInvalid"): Rule => (v, t) =>
    (!isBlank(v) && !re.test(str(v)) ? t(key) : null),

  email: (): Rule => (v, t) => (isEmail(str(v)) ? null : t("validation.emailInvalid")),
  phone: (): Rule => (v, t) => (isPhone(str(v)) ? null : t("validation.phoneInvalid")),
  url: (): Rule => (v, t) => (isUrl(str(v)) ? null : t("validation.urlInvalid")),
  personName: (): Rule => (v, t) => (isPersonName(str(v)) ? null : t("validation.nameChars")),
  address: (): Rule => (v, t) => (isBlank(v) || isColombianAddress(str(v)) ? null : t("validation.addressFormat")),
  slug: (): Rule => (v, t) => (isSlug(str(v)) ? null : t("validation.slugInvalid")),
  sku: (): Rule => (v, t) => (isSku(str(v)) ? null : t("validation.skuInvalid")),

  number: (opts: { min?: number; max?: number } = {}): Rule => (v, t) => {
    const n = num(v);
    if (n === null) return null;
    if (Number.isNaN(n)) return t("validation.numberInvalid");
    if (opts.min != null && n < opts.min) return t("validation.numberMin", { n: opts.min });
    if (opts.max != null && n > opts.max) return t("validation.numberMax", { n: opts.max });
    return null;
  },
  integer: (): Rule => (v, t) => {
    const n = num(v);
    if (n === null) return null;
    return Number.isInteger(n) ? null : t("validation.integerInvalid");
  },
  currency: (opts: { min?: number; max?: number } = {}): Rule => (v, t) => {
    const n = num(v);
    if (n === null) return null;
    if (Number.isNaN(n) || n < 0) return t("validation.currencyInvalid");
    if (opts.min != null && n < opts.min) return t("validation.numberMin", { n: opts.min });
    if (opts.max != null && n > opts.max) return t("validation.numberMax", { n: opts.max });
    return null;
  },
  enumOf: (values: readonly string[]): Rule => (v, t) =>
    (isBlank(v) || values.includes(str(v)) ? null : t("validation.enumInvalid")),
  json: (): Rule => (v, t) => {
    const s = str(v);
    if (s === "") return null;
    try { JSON.parse(s); return null; } catch { return t("validation.jsonInvalid"); }
  },
};

/** Corre las reglas de un campo en orden; devuelve el primer error o null. */
export function check(value: unknown, t: TFunction, ...rs: Rule[]): string | null {
  for (const r of rs) {
    const e = r(value, t);
    if (e) return e;
  }
  return null;
}

export type Schema<T> = Partial<Record<keyof T, Rule[]>>;

/** Valida un objeto contra un esquema por campo. Devuelve `{ campo: mensaje }`. */
export function validateSchema<T extends Record<string, unknown>>(
  value: T, schema: Schema<T>, t: TFunction,
): Record<string, string> {
  const e: Record<string, string> = {};
  for (const key in schema) {
    const rs = schema[key];
    if (!rs) continue;
    const err = check(value[key], t, ...rs);
    if (err) e[key] = err;
  }
  return e;
}
