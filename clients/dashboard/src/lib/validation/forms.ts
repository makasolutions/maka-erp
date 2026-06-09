import type { TFunction } from "i18next";
import type { PartyAddress, PartyContact } from "@/api/parties";
import type { PartyFormValue } from "@/components/party/PartyForm";
import { isContactBlank } from "@/components/party/PartyForm";
import { isEmail, isPhone, isUrl, isPersonName, isColombianAddress, isColombianId, isPastDate, ageInYears } from "./predicates";

/** Edad mínima de una persona de contacto comercial (regla de negocio Maka, §17). */
export const MIN_CONTACT_AGE = 15;

/**
 * Validadores de entidad (tercero/identidad). Las primitivas por tipo viven en
 * predicates.ts (espejo de FormValidationRules.cs) y rules.ts (componibles).
 * Mensajes vía `t` del namespace `common` (claves `validation.*`). Ver §17 en CLAUDE.md.
 */
export { isEmail, isPhone, isUrl, isPersonName, isColombianAddress };

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

  // Regla de negocio: una empresa (Jurídica) debe tener al menos un contacto (§17/§18).
  if (v.kind === "Juridica" && !v.contacts.some((c) => !isContactBlank(c))) {
    e.contacts = t("validation.contactRequiredForCompany");
  }
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
    const key = (f: string) => `contacts.${i}.${f}`;

    // Nombres
    if ((c.firstName ?? "").trim().length > 50) e[key("firstName")] = t("validation.nameMax");
    else if (!isPersonName(c.firstName)) e[key("firstName")] = t("validation.nameChars");
    if ((c.lastName ?? "").trim().length > 50) e[key("lastName")] = t("validation.nameMax");
    else if (!isPersonName(c.lastName)) e[key("lastName")] = t("validation.nameChars");

    // Identificación según tipo (no letras en numéricos, regex por tipo)
    if ((c.identificationNumber ?? "").trim() && !isColombianId(c.identificationNumber, c.identificationTypeCode))
      e[key("identificationNumber")] = t("validation.idInvalid");

    // Fecha de nacimiento: pasada + edad mínima de contacto
    if ((c.birthDate ?? "").trim()) {
      if (!isPastDate(c.birthDate)) e[key("birthDate")] = t("validation.birthFuture");
      else {
        const age = ageInYears(c.birthDate);
        if (age != null && age < MIN_CONTACT_AGE) e[key("birthDate")] = t("validation.minAge", { n: MIN_CONTACT_AGE });
      }
    }

    // Canales requeridos
    if (!(c.email ?? "").trim()) e[key("email")] = t("validation.required");
    else if (!isEmail(c.email)) e[key("email")] = t("validation.emailInvalid");
    if (!(c.cell ?? "").trim()) e[key("cell")] = t("validation.required");
    else if (!isPhone(c.cell)) e[key("cell")] = t("validation.phoneInvalid");
  });
}
