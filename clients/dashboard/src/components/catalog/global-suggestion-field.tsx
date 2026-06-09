/**
 * GlobalSuggestionField — a name input that, as you type (debounced), fuzzy-searches the
 * global catalog and offers to adopt a match. Typo/accent/synonym tolerant (backend uses
 * unaccent + pg_trgm + aliases). Used by the Category and Brand create flows.
 */
import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Check, Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/cn";

export interface SuggestionItem {
  key: string;
  primary: string;
  secondary?: string | null;
  adopted: boolean;
}

interface Props<T> {
  id: string;
  value: string;
  onChange: (v: string) => void;
  search: (q: string) => Promise<T[]>;
  queryKey: string;
  toItem: (item: T) => SuggestionItem;
  onPick: (item: T) => void;
  placeholder?: string;
  disabled?: boolean;
  maxLength?: number;
  autoFocus?: boolean;
}

export function GlobalSuggestionField<T>({
  id, value, onChange, search, queryKey, toItem, onPick, placeholder, disabled, maxLength, autoFocus,
}: Props<T>) {
  const { t } = useTranslation("catalog");
  const [debounced, setDebounced] = useState(value);
  const [open, setOpen] = useState(false);
  const justPicked = useRef(false);

  useEffect(() => {
    if (justPicked.current) { justPicked.current = false; return; }
    const id = setTimeout(() => setDebounced(value), 300);
    return () => clearTimeout(id);
  }, [value]);

  const q = debounced.trim();
  const { data, isFetching } = useQuery({
    queryKey: ["catalog", "global-suggest", queryKey, q],
    queryFn: () => search(q),
    enabled: open && q.length >= 2,
    staleTime: 60_000,
  });

  const items = (data ?? []).map((raw) => ({ raw, ui: toItem(raw) }));

  return (
    <div className="relative">
      <div className="relative">
        <Input
          id={id}
          value={value}
          onChange={(e) => { onChange(e.target.value); setOpen(true); }}
          onFocus={() => setOpen(true)}
          onBlur={() => setTimeout(() => setOpen(false), 150)}
          placeholder={placeholder}
          disabled={disabled}
          maxLength={maxLength}
          autoFocus={autoFocus}
          autoComplete="off"
          className="pr-8"
        />
        <Search className="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-[var(--color-muted-foreground)]" />
      </div>

      {open && q.length >= 2 && (
        <div className="absolute z-50 mt-1 w-full overflow-hidden rounded-lg border border-[var(--color-border)] bg-[var(--color-popover)] shadow-lg">
          <div className="border-b border-[var(--color-border)] px-3 py-1.5 text-[11px] font-semibold uppercase tracking-wide text-[var(--color-muted-foreground)]">
            {t("globalSuggest.title")}
          </div>
          {isFetching && items.length === 0 ? (
            <p className="px-3 py-2.5 text-[12.5px] text-[var(--color-muted-foreground)]">{t("globalSuggest.searching")}</p>
          ) : items.length === 0 ? (
            <p className="px-3 py-2.5 text-[12.5px] text-[var(--color-muted-foreground)]">{t("globalSuggest.none")}</p>
          ) : (
            <ul className="max-h-64 overflow-y-auto">
              {items.map(({ raw, ui }) => (
                <li key={ui.key}>
                  <button
                    type="button"
                    onMouseDown={(e) => { e.preventDefault(); justPicked.current = true; onPick(raw); setOpen(false); }}
                    className={cn(
                      "flex w-full items-center justify-between gap-2 px-3 py-2 text-left hover:bg-[var(--color-muted)]",
                    )}
                  >
                    <span className="min-w-0">
                      <span className="block truncate text-[13px] font-medium text-[var(--color-foreground)]">{ui.primary}</span>
                      {ui.secondary && (
                        <span className="block truncate text-[11.5px] text-[var(--color-muted-foreground)]">{ui.secondary}</span>
                      )}
                    </span>
                    {ui.adopted && (
                      <span className="flex shrink-0 items-center gap-1 rounded-full bg-[var(--color-success)]/15 px-2 py-0.5 text-[10.5px] font-semibold text-[var(--color-success)]">
                        <Check className="size-3" />{t("globalSuggest.adopted")}
                      </span>
                    )}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
