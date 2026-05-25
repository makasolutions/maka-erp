/**
 * LocalizationSettings — Settings > Localización tab.
 *
 * Shows two card sections (Date & Time / Region & Language) plus a live
 * preview strip. All changes are held in local state and persisted only on
 * "Save". On save success the LocalizationContext cache and query are updated
 * via `useLocalization().update()`.
 */

import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Check, Globe } from "lucide-react";
import { toast } from "sonner";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { useLocalization, type LocalizationConfig } from "@/hooks/use-localization";
import { cn } from "@/lib/cn";

// ─── Timezone data ────────────────────────────────────────────────────────────

type TzGroup = { regionKey: string; zones: { value: string; label: string }[] };

const TIMEZONE_GROUPS: TzGroup[] = [
  {
    regionKey: "regions.utc",
    zones: [{ value: "UTC", label: "UTC ±0:00" }],
  },
  {
    regionKey: "regions.latinAmerica",
    zones: [
      { value: "America/Bogota",      label: "Bogotá, Lima (UTC-5)" },
      { value: "America/Guayaquil",   label: "Quito (UTC-5)" },
      { value: "America/Lima",        label: "Lima (UTC-5)" },
      { value: "America/Panama",      label: "Panamá (UTC-5)" },
      { value: "America/Costa_Rica",  label: "San José (UTC-6)" },
      { value: "America/Managua",     label: "Managua (UTC-6)" },
      { value: "America/Tegucigalpa", label: "Tegucigalpa (UTC-6)" },
      { value: "America/El_Salvador", label: "San Salvador (UTC-6)" },
      { value: "America/Guatemala",   label: "Guatemala (UTC-6)" },
      { value: "America/Mexico_City", label: "Ciudad de México (UTC-6)" },
      { value: "America/Monterrey",   label: "Monterrey (UTC-6)" },
      { value: "America/Cancun",      label: "Cancún (UTC-5)" },
      { value: "America/Havana",      label: "La Habana (UTC-5)" },
      { value: "America/Caracas",     label: "Caracas (UTC-4)" },
      { value: "America/La_Paz",      label: "La Paz (UTC-4)" },
      { value: "America/Santiago",    label: "Santiago (UTC-3/-4)" },
      { value: "America/Asuncion",    label: "Asunción (UTC-3/-4)" },
      { value: "America/Sao_Paulo",   label: "São Paulo (UTC-3)" },
      { value: "America/Buenos_Aires",label: "Buenos Aires (UTC-3)" },
      { value: "America/Montevideo",  label: "Montevideo (UTC-3)" },
      { value: "America/Manaus",      label: "Manaos (UTC-4)" },
      { value: "America/Noronha",     label: "Noronha (UTC-2)" },
    ],
  },
  {
    regionKey: "regions.northAmerica",
    zones: [
      { value: "America/New_York",   label: "Eastern — New York (UTC-5)" },
      { value: "America/Chicago",    label: "Central — Chicago (UTC-6)" },
      { value: "America/Denver",     label: "Mountain — Denver (UTC-7)" },
      { value: "America/Phoenix",    label: "Mountain — Phoenix (UTC-7, sin DST)" },
      { value: "America/Los_Angeles",label: "Pacific — Los Ángeles (UTC-8)" },
      { value: "America/Anchorage",  label: "Alaska — Anchorage (UTC-9)" },
      { value: "Pacific/Honolulu",   label: "Hawái (UTC-10)" },
    ],
  },
  {
    regionKey: "regions.europe",
    zones: [
      { value: "Europe/London",    label: "Londres (UTC+0/+1)" },
      { value: "Europe/Lisbon",    label: "Lisboa (UTC+0/+1)" },
      { value: "Europe/Madrid",    label: "Madrid (UTC+1/+2)" },
      { value: "Europe/Paris",     label: "París (UTC+1/+2)" },
      { value: "Europe/Berlin",    label: "Berlín (UTC+1/+2)" },
      { value: "Europe/Rome",      label: "Roma (UTC+1/+2)" },
      { value: "Europe/Amsterdam", label: "Ámsterdam (UTC+1/+2)" },
      { value: "Europe/Zurich",    label: "Zúrich (UTC+1/+2)" },
      { value: "Europe/Warsaw",    label: "Varsovia (UTC+1/+2)" },
      { value: "Europe/Prague",    label: "Praga (UTC+1/+2)" },
      { value: "Europe/Athens",    label: "Atenas (UTC+2/+3)" },
      { value: "Europe/Helsinki",  label: "Helsinki (UTC+2/+3)" },
      { value: "Europe/Stockholm", label: "Estocolmo (UTC+1/+2)" },
      { value: "Europe/Moscow",    label: "Moscú (UTC+3)" },
      { value: "Europe/Istanbul",  label: "Estambul (UTC+3)" },
    ],
  },
  {
    regionKey: "regions.middleEastAsia",
    zones: [
      { value: "Asia/Dubai",      label: "Dubái, Abu Dabi (UTC+4)" },
      { value: "Asia/Riyadh",     label: "Riad (UTC+3)" },
      { value: "Asia/Tehran",     label: "Teherán (UTC+3:30)" },
      { value: "Asia/Karachi",    label: "Karachi (UTC+5)" },
      { value: "Asia/Kolkata",    label: "Mumbai, Nueva Delhi (UTC+5:30)" },
      { value: "Asia/Dhaka",      label: "Dacca (UTC+6)" },
      { value: "Asia/Bangkok",    label: "Bangkok, Yakarta (UTC+7)" },
      { value: "Asia/Singapore",  label: "Singapur, Kuala Lumpur (UTC+8)" },
      { value: "Asia/Shanghai",   label: "Shanghái, Beijing (UTC+8)" },
      { value: "Asia/Hong_Kong",  label: "Hong Kong (UTC+8)" },
      { value: "Asia/Tokyo",      label: "Tokio (UTC+9)" },
      { value: "Asia/Seoul",      label: "Seúl (UTC+9)" },
    ],
  },
  {
    regionKey: "regions.africa",
    zones: [
      { value: "Africa/Lagos",        label: "Lagos (UTC+1)" },
      { value: "Africa/Cairo",        label: "El Cairo (UTC+2)" },
      { value: "Africa/Nairobi",      label: "Nairobi (UTC+3)" },
      { value: "Africa/Johannesburg", label: "Johannesburgo (UTC+2)" },
    ],
  },
  {
    regionKey: "regions.oceania",
    zones: [
      { value: "Australia/Sydney",   label: "Sídney (UTC+10/+11)" },
      { value: "Australia/Brisbane", label: "Brisbane (UTC+10)" },
      { value: "Australia/Perth",    label: "Perth (UTC+8)" },
      { value: "Pacific/Auckland",   label: "Auckland (UTC+12/+13)" },
    ],
  },
];

// ─── Small option-pill component ─────────────────────────────────────────────

function OptionPill({
  selected,
  onClick,
  children,
}: {
  selected: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cn(
        "inline-flex h-9 items-center gap-1.5 rounded-lg border px-3 text-[13px] font-medium",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
        selected
          ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]"
          : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:border-[var(--color-border-strong)] hover:text-[var(--color-foreground)]",
      )}
    >
      {selected && <Check className="size-3.5 shrink-0" aria-hidden />}
      {children}
    </button>
  );
}

// ─── Timezone select ──────────────────────────────────────────────────────────

function TimezoneSelect({
  value,
  onChange,
  groups,
}: {
  value: string;
  onChange: (v: string) => void;
  groups: TzGroup[];
}) {
  const { t } = useTranslation("settings");
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className={cn(
        "h-9 w-full rounded-lg border border-[var(--color-border)] bg-[var(--color-card)]",
        "px-3 text-[13px] text-[var(--color-foreground)]",
        "focus:border-[var(--color-primary)] focus:outline-none focus:ring-2 focus:ring-[var(--color-ring)]",
        "transition-colors duration-[var(--duration-fast)]",
      )}
    >
      {groups.map((group) => (
        <optgroup key={group.regionKey} label={t(`localization.${group.regionKey}`)}>
          {group.zones.map((zone) => (
            <option key={zone.value} value={zone.value}>
              {zone.label}
            </option>
          ))}
        </optgroup>
      ))}
    </select>
  );
}

// ─── Live preview ─────────────────────────────────────────────────────────────

const PREVIEW_DATE = new Date("2026-05-25T14:39:00Z");
const PREVIEW_AMOUNT = 1_250_000;
const PREVIEW_NUMBER = 1_234_567.89;

function LocalizationPreview({ draft }: { draft: LocalizationConfig }) {
  const { t } = useTranslation("settings");

  function buildDraftFormatters() {
    const locale = draft.language === "es" ? "es-CO" : "en-US";
    const dateOpts: Intl.DateTimeFormatOptions = (() => {
      switch (draft.dateFormat) {
        case "MM/DD/YYYY":
          return { month: "2-digit", day: "2-digit", year: "numeric" };
        case "YYYY-MM-DD":
          return { year: "numeric", month: "2-digit", day: "2-digit" };
        default:
          return { day: "2-digit", month: "2-digit", year: "numeric" };
      }
    })();

    const dtFmt = new Intl.DateTimeFormat(locale, {
      timeZone: draft.timezone,
      ...dateOpts,
      hour: "2-digit",
      minute: "2-digit",
      hour12: draft.timeFormat === "12h",
    });
    const dFmt = new Intl.DateTimeFormat(locale, { timeZone: draft.timezone, ...dateOpts });
    const curFmt = new Intl.NumberFormat(locale, {
      style: "currency",
      currency: draft.currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    });
    const numFmt = new Intl.NumberFormat(locale, {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
    return { dtFmt, dFmt, curFmt, numFmt };
  }

  let dtStr = "—", dStr = "—", curStr = "—", numStr = "—";
  try {
    const { dtFmt, dFmt, curFmt, numFmt } = buildDraftFormatters();
    dtStr = dtFmt.format(PREVIEW_DATE);
    dStr = dFmt.format(PREVIEW_DATE);
    curStr = curFmt.format(PREVIEW_AMOUNT);
    numStr = numFmt.format(PREVIEW_NUMBER);
  } catch {
    /* invalid config during transition */
  }

  const rows: { key: string; value: string }[] = [
    { key: t("localization.previewDateTime"), value: dtStr },
    { key: t("localization.previewDate"), value: dStr },
    { key: t("localization.previewCurrency"), value: curStr },
    { key: t("localization.previewNumber"), value: numStr },
  ];

  return (
    <Card className="border-dashed">
      <CardHeader className="pb-2">
        <CardTitle className="text-[13px]">{t("localization.preview")}</CardTitle>
        <CardDescription className="text-[12px]">{t("localization.previewDesc")}</CardDescription>
      </CardHeader>
      <CardContent className="pb-5 pt-2">
        <dl className="grid grid-cols-2 gap-x-6 gap-y-2 sm:grid-cols-4">
          {rows.map(({ key, value }) => (
            <div key={key}>
              <dt className="text-[11px] text-[var(--color-muted-foreground)]">{key}</dt>
              <dd className="mt-0.5 font-mono text-[13px] font-semibold text-[var(--color-foreground)]">
                {value}
              </dd>
            </div>
          ))}
        </dl>
      </CardContent>
    </Card>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export function LocalizationSettings() {
  const { t } = useTranslation("settings");
  const { config, update } = useLocalization();

  // Local draft — only pushed to server on Save.
  const [draft, setDraft] = useState<LocalizationConfig>({ ...config });
  const [isSaving, setIsSaving] = useState(false);

  function patch<K extends keyof LocalizationConfig>(key: K, value: LocalizationConfig[K]) {
    setDraft((prev) => ({ ...prev, [key]: value }));
  }

  async function handleSave() {
    setIsSaving(true);
    try {
      await update(draft);
      toast.success(t("localization.saved"));
    } catch {
      toast.error(t("localization.saveFailed"));
    } finally {
      setIsSaving(false);
    }
  }

  const saveFooter = (
    <div className="flex justify-end">
      <Button
        size="sm"
        onClick={() => void handleSave()}
        disabled={isSaving}
      >
        {isSaving ? t("localization.saving") : t("localization.save")}
      </Button>
    </div>
  );

  return (
    <div className="space-y-6 fsh-enter">
      {/* ── Date & Time ── */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Globe className="size-4 text-[var(--color-muted-foreground)]" aria-hidden />
            {t("localization.dateTime")}
          </CardTitle>
          <CardDescription>{t("localization.dateTimeDesc")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-5 pb-1">
          {/* Timezone */}
          <div className="space-y-1.5">
            <label className="block text-[12px] font-semibold text-[var(--color-foreground)]">
              {t("localization.timezone")}
            </label>
            <p className="text-[11px] text-[var(--color-muted-foreground)]">
              {t("localization.timezoneDesc")}
            </p>
            <TimezoneSelect
              value={draft.timezone}
              onChange={(v) => patch("timezone", v)}
              groups={TIMEZONE_GROUPS}
            />
          </div>

          {/* Date format */}
          <div className="space-y-1.5">
            <label className="block text-[12px] font-semibold text-[var(--color-foreground)]">
              {t("localization.dateFormat")}
            </label>
            <div className="flex flex-wrap gap-2">
              {(["DD/MM/YYYY", "MM/DD/YYYY", "YYYY-MM-DD"] as const).map((fmt) => (
                <OptionPill
                  key={fmt}
                  selected={draft.dateFormat === fmt}
                  onClick={() => patch("dateFormat", fmt)}
                >
                  {fmt}
                </OptionPill>
              ))}
            </div>
          </div>

          {/* Time format */}
          <div className="space-y-1.5">
            <label className="block text-[12px] font-semibold text-[var(--color-foreground)]">
              {t("localization.timeFormat")}
            </label>
            <div className="flex flex-wrap gap-2">
              {(["12h", "24h"] as const).map((fmt) => (
                <OptionPill
                  key={fmt}
                  selected={draft.timeFormat === fmt}
                  onClick={() => patch("timeFormat", fmt)}
                >
                  {fmt === "12h"
                    ? t("localization.timeFormats.12h")
                    : t("localization.timeFormats.24h")}
                </OptionPill>
              ))}
            </div>
          </div>
        </CardContent>
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-6 py-3">
          {saveFooter}
        </div>
      </Card>

      {/* ── Region & Language ── */}
      <Card>
        <CardHeader>
          <CardTitle>{t("localization.regionLanguage")}</CardTitle>
          <CardDescription>{t("localization.regionLanguageDesc")}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-5 pb-1">
          {/* Language */}
          <div className="space-y-1.5">
            <label className="block text-[12px] font-semibold text-[var(--color-foreground)]">
              {t("localization.language")}
            </label>
            <div className="flex flex-wrap gap-2">
              {(["es", "en"] as const).map((lang) => (
                <OptionPill
                  key={lang}
                  selected={draft.language === lang}
                  onClick={() => patch("language", lang)}
                >
                  {lang === "es" ? "🇨🇴 Español" : "🇺🇸 English"}
                </OptionPill>
              ))}
            </div>
          </div>

          {/* Currency */}
          <div className="space-y-1.5">
            <label className="block text-[12px] font-semibold text-[var(--color-foreground)]">
              {t("localization.currency")}
            </label>
            <p className="text-[11px] text-[var(--color-muted-foreground)]">
              {t("localization.currencyDesc")}
            </p>
            <div className="flex flex-wrap gap-2">
              {(["COP", "USD", "EUR"] as const).map((cur) => (
                <OptionPill
                  key={cur}
                  selected={draft.currency === cur}
                  onClick={() => patch("currency", cur)}
                >
                  {t(`localization.currencies.${cur}`)}
                </OptionPill>
              ))}
            </div>
          </div>

          {/* Number format */}
          <div className="space-y-1.5">
            <label className="block text-[12px] font-semibold text-[var(--color-foreground)]">
              {t("localization.numberFormat")}
            </label>
            <p className="text-[11px] text-[var(--color-muted-foreground)]">
              {t("localization.numberFormatDesc")}
            </p>
            <div className="flex flex-wrap gap-2">
              <OptionPill
                selected={draft.numberFormat === "1.000,00"}
                onClick={() => patch("numberFormat", "1.000,00")}
              >
                {t("localization.numberFormats.dotComma")}
              </OptionPill>
              <OptionPill
                selected={draft.numberFormat === "1,000.00"}
                onClick={() => patch("numberFormat", "1,000.00")}
              >
                {t("localization.numberFormats.commaDot")}
              </OptionPill>
            </div>
          </div>
        </CardContent>
        <div className="border-t border-[oklch(from_var(--color-border)_l_c_h_/_0.5)] px-6 py-3">
          {saveFooter}
        </div>
      </Card>

      {/* ── Live preview ── */}
      <LocalizationPreview draft={draft} />
    </div>
  );
}
