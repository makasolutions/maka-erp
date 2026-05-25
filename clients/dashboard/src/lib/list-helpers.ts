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

export function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

// Surface API/network/runtime errors with the same formatting everywhere.
// Prefers the Dev-only `reason` extension on ProblemDetails so JwtBearer
// rejection causes (expired token, signing key drift, etc) are visible
// in toast descriptions during development.
export function describe(err: unknown): string {
  if (err instanceof ApiRequestError) {
    const reason =
      err.problem?.reason ??
      err.problem?.detail ??
      err.problem?.title ??
      err.message;
    return `${err.status} ${reason}`;
  }
  if (err instanceof Error) return err.message;
  return String(err);
}
