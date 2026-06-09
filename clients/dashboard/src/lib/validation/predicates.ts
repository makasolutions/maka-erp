/**
 * Predicados de validación puros (sin dependencias de componentes), espejo 1:1 de
 * FormValidationRules.cs en el backend (fuente de verdad). Consumidos por forms.ts
 * (validadores de entidad) y rules.ts (primitivas componibles por tipo).
 */

export const PERSON_NAME = /^[\p{L}][\p{L}\p{M}\s.'-]*$/u;
export const EXCESS_REPEAT = /(.)\1{7,}/u;
export const PHONE = /^(3\d{9}|\+\d{7,15})$/;
// Tolerante: separador "#", "No"/"No."/"Nro"/"N°"/"Número"; números con sufijo de letra.
export const ADDRESS =
  /^(CL|CALLE|KR|CR|CRA|CARRERA|AV|AVENIDA|AC|AK|DG|DIAGONAL|TV|TRANSV|TRANSVERSAL|CQ|CIRCULAR|CV|CIRCUNVALAR|AU|AUTOPISTA|KM|MZ|MANZANA|VRD|VEREDA)\.?\s+\S+.*(#|N(?:[O°º]|RO|[UÚ]MERO)?\.?)\s*\d+[A-Z]?\s*-\s*\d+[A-Z]?/i;
export const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
// Slug URL-amigable: minúsculas, dígitos y guiones; sin guiones al borde ni dobles.
export const SLUG = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
// SKU/código: alfanumérico en mayúsculas con - _ . / ; 1–64.
export const SKU = /^[A-Za-z0-9][A-Za-z0-9._/-]{0,63}$/;

export const isBlank = (v: unknown): boolean =>
  v == null || (typeof v === "string" && v.trim() === "");

export const isPersonName = (s?: string | null): boolean => {
  const v = (s ?? "").trim();
  return v === "" || (PERSON_NAME.test(v) && !EXCESS_REPEAT.test(v));
};

export const isUrl = (s?: string | null): boolean => {
  const raw = (s ?? "").trim();
  if (raw === "") return true;
  const withScheme = raw.includes("://") ? raw : `https://${raw}`;
  try {
    const u = new URL(withScheme);
    return (u.protocol === "http:" || u.protocol === "https:") && u.hostname.includes(".") && !u.hostname.includes(" ");
  } catch {
    return false;
  }
};

export const isPhone = (s?: string | null): boolean => {
  const v = (s ?? "").trim();
  if (v === "") return true;
  return PHONE.test(v.replace(/[\s\-()]/g, ""));
};

export const isEmail = (s?: string | null): boolean => {
  const v = (s ?? "").trim();
  return v === "" || EMAIL.test(v);
};

export const isColombianAddress = (s?: string | null): boolean => {
  const v = (s ?? "").trim();
  return v !== "" && ADDRESS.test(v);
};

export const isSlug = (s?: string | null): boolean => {
  const v = (s ?? "").trim();
  return v === "" || SLUG.test(v);
};

export const isSku = (s?: string | null): boolean => {
  const v = (s ?? "").trim();
  return v === "" || SKU.test(v);
};

// ── Identificación colombiana por tipo de documento (§17) ────────────────────
export const ID_REGEX: Record<string, RegExp> = {
  CC: /^\d{6,10}$/,
  TI: /^\d{8,11}$/,
  NUIP: /^\d{8,11}$/,
  CE: /^[A-Za-z0-9]{6,15}$/,
  NIT: /^\d{9,10}$/,
  NIT_EXT: /^[A-Za-z0-9]{5,20}$/,
  PASAPORTE: /^[A-Za-z0-9]{6,15}$/,
};
/** Tipos de documento que son estrictamente numéricos (máscara solo-dígitos). */
export const isNumericIdType = (type?: string | null): boolean =>
  ["CC", "TI", "NUIP", "NIT"].includes((type ?? "").trim().toUpperCase());

/** Valida un número de identificación según su tipo. Vacío = válido (requerido aparte). */
export const isColombianId = (value?: string | null, type?: string | null): boolean => {
  const v = (value ?? "").trim();
  if (v === "") return true;
  const re = ID_REGEX[(type ?? "").trim().toUpperCase()];
  return re ? re.test(v) : /^[A-Za-z0-9]{4,20}$/.test(v);
};

// ── Fechas ───────────────────────────────────────────────────────────────────
/** Parse ISO `yyyy-MM-dd` (o cualquier Date-string) a Date local, o null. */
const parseDate = (iso?: string | null): Date | null => {
  const v = (iso ?? "").trim();
  if (v === "") return null;
  const d = new Date(v);
  return Number.isNaN(d.getTime()) ? null : d;
};
const startOfToday = (): Date => {
  const d = new Date();
  d.setHours(0, 0, 0, 0);
  return d;
};
/** Años cumplidos a partir de una fecha de nacimiento ISO. */
export const ageInYears = (iso?: string | null): number | null => {
  const d = parseDate(iso);
  if (!d) return null;
  const today = new Date();
  let age = today.getFullYear() - d.getFullYear();
  const m = today.getMonth() - d.getMonth();
  if (m < 0 || (m === 0 && today.getDate() < d.getDate())) age--;
  return age;
};
/** true si la fecha NO es futura (≤ hoy). Vacío = válido. */
export const isNotFutureDate = (iso?: string | null): boolean => {
  const d = parseDate(iso);
  return !d || d <= new Date();
};
/** true si es estrictamente anterior a hoy (00:00). Vacío = válido. */
export const isPastDate = (iso?: string | null): boolean => {
  const d = parseDate(iso);
  return !d || d < startOfToday();
};
