import type { TFunction } from "i18next";
import type { PartyAddress, PartyContact } from "@/api/parties";
import type { PartyFormValue } from "@/components/party/PartyForm";
import { isContactBlank } from "@/components/party/PartyForm";

/**
 * Reglas de validación del frontend, espejo 1:1 de FormValidationRules.cs en el
 * backend (fuente de verdad). Devuelven mensajes traducidos vía el `t` del
 * namespace `common` (claves bajo `validation.*`). Ver §"Estándar de validación
 * de formularios" en CLAUDE.md.
 */

const PERSON_NAME = /^[\p{L}][\p{L}\p{M}\s.'-]*$/u;
const EXCESS_REPEAT = /(.)\1{7,}/u;
const PHONE = /^(3\d{9}|\+\d{7,15})$/;
// Tolerante: separador "#", "No"/"No."/"Nro"/"N°"/"Número"; números con sufijo de
// letra ("131A", "53C"). Ej. válidos: "Carrera 53C No 131A - 91", "KR 7 # 12-34".
const ADDRESS =
  /^(CL|CALLE|KR|CR|CRA|CARRERA|AV|AVENIDA|AC|AK|DG|DIAGONAL|TV|TRANSV|TRANSVERSAL|CQ|CIRCULAR|CV|CIRCUNVALAR|AU|AUTOPISTA|KM|MZ|MANZANA|VRD|VEREDA)\.?\s+\S+.*(#|N(?:[O°º]|RO|[UÚ]MERO)?\.?)\s*\d+[A-Z]?\s*-\s*\d+[A-Z]?/i;
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

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

/** Mensaje de error de fecha de nacimiento, o null si es válida. */
export const birthDateError = (iso: string | null | undefined, t: TFunction): string | null => {
  if (!iso) return null;
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return t("validation.dateInvalid");
  const today = new Date();
  if (d > today) return t("validation.birthFuture");
  const min = new Date();
  min.setFullYear(min.getFullYear() - 120);
  if (d < min) return t("validation.birthInvalid");
  return null;
};

/**
 * Valida un tercero completo. Devuelve un mapa clave→mensaje. Claves de hijos
 * usan notación `addresses.{i}.{campo}` / `contacts.{i}.{campo}` (índice del
 * arreglo crudo, no del filtrado).
 */
export function validateParty(v: PartyFormValue, t: TFunction): Record<string, string> {
  const e: Record<string, string> = {};

  if (!v.identificationTypeCode) e.identificationTypeCode = t("validation.required");
  if (!v.identificationNumber.trim()) e.identificationNumber = t("validation.required");

  if (v.kind === "Juridica") {
    if (!v.legalName.trim()) e.legalName = t("validation.required");
    else if (v.legalName.trim().length > 150) e.legalName = t("validation.legalNameMax");
  } else {
    if (!v.firstName.trim()) e.firstName = t("validation.required");
    else if (v.firstName.trim().length > 50) e.firstName = t("validation.nameMax");
    else if (!isPersonName(v.firstName)) e.firstName = t("validation.nameChars");
    if (!v.lastName.trim()) e.lastName = t("validation.required");
    else if (v.lastName.trim().length > 50) e.lastName = t("validation.nameMax");
    else if (!isPersonName(v.lastName)) e.lastName = t("validation.nameChars");
  }

  if (v.email.trim() && !isEmail(v.email)) e.email = t("validation.emailInvalid");
  if (v.website.trim() && !isUrl(v.website)) e.website = t("validation.urlInvalid");

  if (v.addresses.length === 0) e.addresses = t("validation.addressAtLeastOne");
  v.addresses.forEach((a, i) => {
    if (!(a.city ?? "").trim()) e[`addresses.${i}.city`] = t("validation.cityRequired");
    if (!(a.line ?? "").trim()) e[`addresses.${i}.line`] = t("validation.required");
    else if (!isColombianAddress(a.line)) e[`addresses.${i}.line`] = t("validation.addressFormat");
    if (a.latitude != null && (a.latitude < -90 || a.latitude > 90)) e[`addresses.${i}.latitude`] = t("validation.latRange");
    if (a.longitude != null && (a.longitude < -180 || a.longitude > 180)) e[`addresses.${i}.longitude`] = t("validation.lngRange");
    if ((a.latitude != null) !== (a.longitude != null)) e[`addresses.${i}.geo`] = t("validation.geoPair");
  });

  validateAddressesAndContacts(v.addresses, v.contacts, e, t);
  return e;
}

/** Forma mínima compartida por el editor de empleados (persona natural). */
export interface IdentityLike {
  identificationTypeCode: string | null;
  identificationNumber: string;
  firstName: string;
  lastName: string;
  email: string;
  addresses: PartyAddress[];
  contacts: PartyContact[];
}

/** Valida la identidad de una persona natural (empleado): nombres, doc, contactos, direcciones. */
export function validateIdentity(v: IdentityLike, t: TFunction): Record<string, string> {
  const e: Record<string, string> = {};

  if (!v.identificationTypeCode) e.identificationTypeCode = t("validation.required");
  if (!v.identificationNumber.trim()) e.identificationNumber = t("validation.required");

  if (!v.firstName.trim()) e.firstName = t("validation.required");
  else if (v.firstName.trim().length > 50) e.firstName = t("validation.nameMax");
  else if (!isPersonName(v.firstName)) e.firstName = t("validation.nameChars");
  if (!v.lastName.trim()) e.lastName = t("validation.required");
  else if (v.lastName.trim().length > 50) e.lastName = t("validation.nameMax");
  else if (!isPersonName(v.lastName)) e.lastName = t("validation.nameChars");

  if (v.email.trim() && !isEmail(v.email)) e.email = t("validation.emailInvalid");

  validateAddressesAndContacts(v.addresses, v.contacts, e, t);
  return e;
}

function validateAddressesAndContacts(
  addresses: PartyAddress[],
  contacts: PartyContact[],
  e: Record<string, string>,
  t: TFunction,
): void {
  if (addresses.length === 0) e.addresses = t("validation.addressAtLeastOne");
  addresses.forEach((a, i) => {
    if (!(a.city ?? "").trim()) e[`addresses.${i}.city`] = t("validation.cityRequired");
    if (!(a.line ?? "").trim()) e[`addresses.${i}.line`] = t("validation.required");
    else if (!isColombianAddress(a.line)) e[`addresses.${i}.line`] = t("validation.addressFormat");
    if (a.latitude != null && (a.latitude < -90 || a.latitude > 90)) e[`addresses.${i}.latitude`] = t("validation.latRange");
    if (a.longitude != null && (a.longitude < -180 || a.longitude > 180)) e[`addresses.${i}.longitude`] = t("validation.lngRange");
    if ((a.latitude != null) !== (a.longitude != null)) e[`addresses.${i}.geo`] = t("validation.geoPair");
  });

  contacts.forEach((c, i) => {
    if (isContactBlank(c)) return;
    if (!(c.email ?? "").trim()) e[`contacts.${i}.email`] = t("validation.required");
    else if (!isEmail(c.email)) e[`contacts.${i}.email`] = t("validation.emailInvalid");
    if (!(c.cell ?? "").trim()) e[`contacts.${i}.cell`] = t("validation.required");
    else if (!isPhone(c.cell)) e[`contacts.${i}.cell`] = t("validation.phoneInvalid");
  });
}
