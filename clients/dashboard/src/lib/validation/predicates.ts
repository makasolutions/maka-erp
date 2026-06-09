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
