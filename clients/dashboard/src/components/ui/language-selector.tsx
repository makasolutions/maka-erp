/**
 * LanguageSelector — switches the dashboard display language between ES and EN.
 *
 * Uses i18next.changeLanguage() which persists the choice via
 * i18next-browser-languagedetector (localStorage key: "i18nextLng").
 */
import { useTranslation } from "react-i18next";
import { Check, Languages } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { cn } from "@/lib/cn";

type Lang = { code: string; label: string; flag: string };

const LANGUAGES: Lang[] = [
  { code: "es", label: "Español", flag: "🇨🇴" },
  { code: "en", label: "English", flag: "🇺🇸" },
];

/**
 * Compact icon-button variant — used in the Topbar.
 * Shows the Languages icon; opens a dropdown with ES/EN choices.
 */
export function LanguageSelectorButton() {
  const { i18n } = useTranslation();
  const current = i18n.resolvedLanguage ?? i18n.language ?? "es";
  const shortLang = current.split("-")[0];

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <button
          type="button"
          aria-label="Change language"
          title="Change language"
          className={cn(
            "grid h-9 w-9 cursor-pointer place-items-center rounded-md",
            "text-[var(--color-muted-foreground)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
            "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
            "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
          )}
        >
          <Languages className="h-4 w-4" aria-hidden />
        </button>
      </DropdownMenuTrigger>

      <DropdownMenuContent align="end" sideOffset={6} className="w-[160px] p-1">
        {LANGUAGES.map((lang) => {
          const isActive = shortLang === lang.code;
          return (
            <DropdownMenuItem
              key={lang.code}
              onSelect={() => void i18n.changeLanguage(lang.code)}
              className="!my-0 flex cursor-pointer items-center gap-2.5 rounded-md !px-2.5 !py-1.5"
            >
              <span className="text-base leading-none" aria-hidden>
                {lang.flag}
              </span>
              <span className="flex-1 text-[12.5px] font-medium text-[var(--color-foreground)]">
                {lang.label}
              </span>
              {isActive && (
                <Check
                  className="size-3.5 shrink-0 text-[var(--color-primary)]"
                  aria-hidden
                />
              )}
            </DropdownMenuItem>
          );
        })}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * Inline select variant — used in Settings > Appearance.
 * Renders as a labelled row of clickable pills.
 */
export function LanguageSelectorInline() {
  const { i18n, t } = useTranslation("settings");
  const current = i18n.resolvedLanguage ?? i18n.language ?? "es";
  const shortLang = current.split("-")[0];

  return (
    <div className="flex items-center gap-2">
      {LANGUAGES.map((lang) => {
        const isActive = shortLang === lang.code;
        return (
          <button
            key={lang.code}
            type="button"
            onClick={() => void i18n.changeLanguage(lang.code)}
            className={cn(
              "inline-flex h-9 items-center gap-2 rounded-lg border px-3 text-[13px] font-medium",
              "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
              isActive
                ? "border-[var(--color-primary)] bg-[oklch(from_var(--color-primary)_l_c_h_/_0.10)] text-[var(--color-primary)]"
                : "border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-muted-foreground)] hover:border-[var(--color-border-strong)] hover:text-[var(--color-foreground)]",
            )}
          >
            <span className="text-base leading-none" aria-hidden>
              {lang.flag}
            </span>
            {lang.label}
            {isActive && (
              <Check className="size-3.5 shrink-0" aria-hidden />
            )}
          </button>
        );
      })}
      <span className="ml-1 text-[11px] text-[var(--color-muted-foreground)]">
        {t("appearance.languageDesc")}
      </span>
    </div>
  );
}
