import { ApiRequestError } from "@/lib/api-client";
import i18next from "i18next";

function getDateFormatter() {
  return new Intl.DateTimeFormat(i18next.language || "es", {
    month: "short",
    day: "2-digit",
    year: "numeric",
  });
}

export function formatDate(iso: string | null | undefined) {
  if (!iso) return "—";
  return getDateFormatter().format(new Date(iso));
}

// "APR 30 2026" — mono-caps tabular form for ledger/registry rows.
export function formatDateMono(iso: string | null | undefined) {
  if (!iso) return "—";
  return getDateFormatter().format(new Date(iso)).toUpperCase().replace(",", "");
}

// "hace 3d", "hace 2mo" — terse relative time for the secondary line.
export function formatRelative(iso: string | null | undefined) {
  if (!iso) return "";
  const diffMs = Date.now() - new Date(iso).getTime();
  if (Number.isNaN(diffMs) || diffMs < 0) return "";
  const t = i18next.t.bind(i18next);
  const sec = Math.floor(diffMs / 1000);
  if (sec < 60) return t("common:relativeTime.justNow");
  const min = Math.floor(sec / 60);
  if (min < 60) return t("common:relativeTime.minutesAgo", { count: min });
  const hr = Math.floor(min / 60);
  if (hr < 24) return t("common:relativeTime.hoursAgo", { count: hr });
  const day = Math.floor(hr / 24);
  if (day < 30) return t("common:relativeTime.daysAgo", { count: day });
  const mo = Math.floor(day / 30);
  if (mo < 12) return t("common:relativeTime.monthsAgo", { count: mo });
  const yr = Math.floor(day / 365);
  return t("common:relativeTime.yearsAgo", { count: yr });
}

export function pad2(n: number) {
  return n.toString().padStart(2, "0");
}

// Mirror the server's slug derivation so editors can show a live preview.
// NFD-normalise first so accented chars (á→a, ñ→n, ü→u) survive intact
// instead of turning into dashes.
export function slugify(value: string) {
  // Decompose into base char + combining diacritic, then strip the
  // diacritics (U+0300–U+036F = Combining Diacritical Marks block).
  const normalized = value
    .trim()
    .normalize("NFD")
    // Covers all combining diacritical marks (U+0300–U+036F)
    .replace(/[̀-ͯ]/g, "")
    .toLowerCase();
  const chars = [...normalized].map((c) => (/[a-z0-9]/.test(c) ? c : "-"));
  let s = chars.join("").replace(/^-+|-+$/g, "");
  while (s.includes("--")) s = s.replace(/--/g, "-");
  return s;
}

/**
 * Canonical Maka money format — the SINGLE source of money rendering site-wide.
 *
 * House style (see CLAUDE.md): symbol "$" first, "." as thousands separator,
 * "," as decimal separator (es-CO grouping); the ISO code, when shown, goes at
 * the END:
 *   formatMoney(12540000)        → "$12.540.000"
 *   formatMoney(12540000, "COP") → "$12.540.000 COP"
 *   formatMoney(1299, "USD")     → "$1.299,00 USD"
 *
 * Decimals: COP (and code-less amounts) render with 0 decimals; other
 * currencies keep 2 so foreign-currency cents aren't silently rounded away.
 * Pass `{ code: false }` to suppress the trailing ISO code even when a currency
 * is given (single-currency screens where the code is redundant).
 */

/**
 * Suggested-price rounding for derived price lists (mirror of backend
 * CatalogPricing.RoundSuggested): round to the nearest $10.000 then subtract
 * $1.000. Never negative. The result is a suggestion the user can edit.
 */
export function roundSuggested(value: number): number {
  if (value <= 0) return 0;
  const rounded = Math.round(value / 10000) * 10000;
  const result = rounded - 1000;
  return result < 0 ? 0 : result;
}

/** Derived price = base × (1 + pct/100), rounded. */
export function derivePrice(base: number, pct: number): number {
  return roundSuggested(base * (1 + pct / 100));
}

/** General VAT (Colombia, 19%). Mirror of backend CatalogPricing.IvaRate. */
export const IVA_RATE = 0.19;
export const toTaxIncluded = (base: number) => base * (1 + IVA_RATE);
export const fromTaxIncluded = (incl: number) => incl / (1 + IVA_RATE);

/**
 * Derived base price computed on the VAT-included value of the default list:
 * inclDerived = inclDefault × (1 ± pct); rounded (10k − 1k) only when `round`;
 * base = inclDerived / (1 + IVA). Mirror of backend CatalogPricing.DeriveBase.
 */
export function deriveBaseFromDefault(baseDefault: number, pct: number, round: boolean): number {
  const inclDefault = toTaxIncluded(baseDefault);
  let inclDerived = inclDefault * (1 + pct / 100);
  if (round) inclDerived = roundSuggested(inclDerived);
  return Math.round(fromTaxIncluded(inclDerived));
}

export function formatMoney(
  amount: number,
  currency?: string | null,
  opts?: { code?: boolean },
): string {
  const code = currency?.trim().toUpperCase() || null;
  const decimals = code && code !== "COP" ? 2 : 0;
  let grouped: string;
  try {
    grouped = new Intl.NumberFormat("es-CO", {
      minimumFractionDigits: decimals,
      maximumFractionDigits: decimals,
    }).format(amount);
  } catch {
    grouped = amount.toFixed(decimals);
  }
  const base = `$${grouped}`;
  return code && opts?.code !== false ? `${base} ${code}` : base;
}

// Surface API/network/runtime errors with the same formatting everywhere.
// Prefers the Dev-only `reason` extension on ProblemDetails so JwtBearer
// rejection causes (expired token, signing key drift, etc) are visible
// in toast descriptions during development.
/**
 * Field-level validation errors from a 400 ProblemDetails (`errors` dictionary),
 * keyed by the backend property name (e.g. "LegalName", "Contacts[0].Email").
 * Empty object when the error isn't a validation error.
 */
export function fieldErrors(err: unknown): Record<string, string[]> {
  if (err instanceof ApiRequestError && err.problem?.errors) {
    return err.problem.errors;
  }
  return {};
}

/** Flat, de-duplicated list of all validation messages from a 400 error. */
export function validationMessages(err: unknown): string[] {
  const map = fieldErrors(err);
  const seen = new Set<string>();
  for (const list of Object.values(map)) {
    for (const m of list) if (m?.trim()) seen.add(m.trim());
  }
  return [...seen];
}

/** Friendly, user-facing message for any error (no status codes, localized). */
export function describe(err: unknown): string {
  if (err instanceof ApiRequestError) {
    // Prefer the specific per-field messages over the generic title.
    const messages = validationMessages(err);
    if (messages.length > 0) return messages.join(" · ");

    const reason = err.problem?.reason ?? err.problem?.detail ?? err.problem?.title ?? err.message;
    // Localize the framework's generic 500 ("An unexpected error occurred…").
    if (err.status >= 500 || /unexpected error occurred/i.test(reason ?? "")) {
      return i18next.t("common:errors.unexpected");
    }
    if (err.status === 401 || err.status === 403) {
      return i18next.t("common:errors.forbidden");
    }
    // Business messages (e.g. "Ya existe un tercero…") are already friendly/localized.
    return reason ?? i18next.t("common:errors.unexpected");
  }
  if (err instanceof Error) return err.message;
  return String(err);
}
