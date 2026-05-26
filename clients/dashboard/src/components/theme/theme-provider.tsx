import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { flushSync } from "react-dom";
import { toast } from "sonner";
import {
  ACCENT_STORAGE_KEY,
  accents,
  buildCustomBrandStops,
  CUSTOM_ACCENT_ID,
  CUSTOM_ACCENT_STORAGE_KEY,
  DEFAULT_ACCENT,
  DEFAULT_CUSTOM_ACCENT,
  DEFAULT_DENSITY,
  DEFAULT_FONT,
  DENSITY_STORAGE_KEY,
  FONT_STORAGE_KEY,
  fonts,
  type CustomAccentSpec,
  type DensityMode,
} from "@/components/theme/appearance-options";
import { updateTenantAppearance, type AppearanceConfig } from "@/api/identity";

export type ThemeMode = "light" | "dark" | "system";
type ResolvedTheme = "light" | "dark";

type ThemeContextValue = {
  mode: ThemeMode;
  resolved: ResolvedTheme;
  setMode: (mode: ThemeMode) => void;
  font: string;
  setFont: (id: string) => void;
  accent: string;
  setAccent: (id: string) => void;
  /** Currently configured custom accent spec — drives the live preview
   *  in the appearance UI even when a preset is selected. */
  customAccent: CustomAccentSpec;
  setCustomAccent: (spec: CustomAccentSpec) => void;
  density: DensityMode;
  setDensity: (next: DensityMode) => void;
  /**
   * Apply all appearance values from the API response without triggering an
   * API re-save. Called by AppearanceSyncer after a successful GET /appearance.
   * Writes to localStorage so the boot cache stays fresh.
   */
  applyFromApiConfig: (config: AppearanceConfig) => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);
const THEME_STORAGE_KEY = "fsh.theme";
const ACCENT_CLASS_PREFIX = "accent-";
const FALLBACK_TRANSITION_MS = 280;

function readStoredMode(): ThemeMode {
  if (typeof window === "undefined") return "system";
  try {
    const stored = window.localStorage.getItem(THEME_STORAGE_KEY);
    return stored === "light" || stored === "dark" || stored === "system" ? stored : "system";
  } catch {
    return "system";
  }
}

function readStoredString(key: string, fallback: string): string {
  if (typeof window === "undefined") return fallback;
  try {
    return window.localStorage.getItem(key) ?? fallback;
  } catch {
    return fallback;
  }
}

function systemPrefersDark(): boolean {
  if (typeof window === "undefined") return false;
  return window.matchMedia("(prefers-color-scheme: dark)").matches;
}

function applyDarkClass(next: ResolvedTheme) {
  document.documentElement.classList.toggle("dark", next === "dark");
}

let fallbackTimer: number | undefined;

/**
 * Wraps a DOM mutation in the smoothest available crossfade:
 *
 *  1. View Transitions API — single bitmap snapshot crossfade. The only
 *     mechanism that can morph gradients, box-shadows, SVG fills,
 *     conic/radial backgrounds, and image-based surfaces in lockstep
 *     with text and bg-color. Handles every property uniformly.
 *  2. Firefox / older browsers — opt into the scoped `theme-switching`
 *     blanket transition for the subset of properties CSS *can*
 *     interpolate.
 *  3. Reduced-motion / cold load — apply instantly, no animation.
 *
 * The commit callback MUST update the DOM synchronously. We use
 * `flushSync` at the call site so React's state updates land inside the
 * View Transitions snapshot window — otherwise the toggle thumb would
 * lead the actual class flip by a frame and the snapshot would be
 * inconsistent with the new state.
 */
function withThemeTransition(commit: () => void): void {
  const root = document.documentElement;
  const ready = root.classList.contains("theme-ready");
  const reduceMotion =
    typeof window !== "undefined" &&
    window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const canViewTransition = typeof document.startViewTransition === "function";

  if (!ready || reduceMotion) {
    commit();
    return;
  }

  if (canViewTransition) {
    // Mark the document for the duration of the transition so component
    // CSS can opt out of competing transitions (e.g. the seg-thumb's own
    // transform animation, which would otherwise interpolate inside the
    // captured snapshot and tear).
    root.classList.add("vt-active");
    const transition = document.startViewTransition!(commit);
    const cleanup = () => root.classList.remove("vt-active");
    transition.finished.then(cleanup, cleanup);
    return;
  }

  root.classList.add("theme-switching");
  if (fallbackTimer !== undefined) window.clearTimeout(fallbackTimer);
  try {
    commit();
  } finally {
    fallbackTimer = window.setTimeout(() => {
      root.classList.remove("theme-switching");
      fallbackTimer = undefined;
    }, FALLBACK_TRANSITION_MS);
  }
}

function applyFont(id: string) {
  if (typeof document === "undefined") return;
  const opt = fonts.find((f) => f.id === id) ?? fonts[0];
  document.documentElement.style.setProperty("--font-sans", opt.family);
}

const BRAND_VARS: ReadonlyArray<string> = [
  "--brand-50", "--brand-100", "--brand-200", "--brand-300", "--brand-400",
  "--brand-500", "--brand-600", "--brand-700", "--brand-800", "--brand-900",
  "--brand-950",
];

function clearCustomBrandInlineStyles(root: HTMLElement) {
  for (const v of BRAND_VARS) root.style.removeProperty(v);
}

function applyCustomBrandInlineStyles(root: HTMLElement, spec: CustomAccentSpec) {
  for (const { var: name, value } of buildCustomBrandStops(spec)) {
    root.style.setProperty(name, value);
  }
}

function applyAccent(id: string, customSpec: CustomAccentSpec) {
  if (typeof document === "undefined") return;
  const root = document.documentElement;
  // Strip any existing accent-* class then add the new one. The default
  // (indigo) lives in :root so it needs no class.
  root.className = root.className
    .split(/\s+/)
    .filter((c) => !c.startsWith(ACCENT_CLASS_PREFIX))
    .join(" ");

  if (id === CUSTOM_ACCENT_ID) {
    // Custom accent — inline-style the eleven --brand-* stops from the
    // current spec. Inline styles win over the accent-* class rules so
    // we don't need to add any class.
    applyCustomBrandInlineStyles(root, customSpec);
    return;
  }

  // Preset (or default). Make sure inline overrides aren't lingering
  // from a previous custom selection — otherwise the chosen preset
  // would be invisible.
  clearCustomBrandInlineStyles(root);

  if (id !== DEFAULT_ACCENT && accents.some((a) => a.id === id)) {
    root.classList.add(`${ACCENT_CLASS_PREFIX}${id}`);
  }
}

function readStoredCustomAccent(): CustomAccentSpec {
  if (typeof window === "undefined") return DEFAULT_CUSTOM_ACCENT;
  try {
    const raw = window.localStorage.getItem(CUSTOM_ACCENT_STORAGE_KEY);
    if (!raw) return DEFAULT_CUSTOM_ACCENT;
    const parsed = JSON.parse(raw) as Partial<CustomAccentSpec>;
    return {
      h: typeof parsed.h === "number" ? parsed.h : DEFAULT_CUSTOM_ACCENT.h,
      c: typeof parsed.c === "number" ? parsed.c : DEFAULT_CUSTOM_ACCENT.c,
    };
  } catch {
    return DEFAULT_CUSTOM_ACCENT;
  }
}

function applyDensity(value: DensityMode) {
  if (typeof document === "undefined") return;
  document.documentElement.classList.toggle("density-compact", value === "compact");
}

/**
 * Clear all appearance-related localStorage keys.
 * Call on logout / tenant switch so the next user doesn't briefly see
 * the previous user's theme before the API response arrives.
 */
export function clearAppearanceCache(): void {
  try {
    localStorage.removeItem(THEME_STORAGE_KEY);
    localStorage.removeItem(FONT_STORAGE_KEY);
    localStorage.removeItem(ACCENT_STORAGE_KEY);
    localStorage.removeItem(CUSTOM_ACCENT_STORAGE_KEY);
    localStorage.removeItem(DENSITY_STORAGE_KEY);
  } catch {
    /* storage unavailable */
  }
}

/** Fire-and-forget: persist appearance to the API. Retries once on transient timeouts. */
async function persistAppearanceToApi(config: AppearanceConfig): Promise<void> {
  const attemptSave = async (): Promise<boolean> => {
    try {
      await updateTenantAppearance(config);
      return true;
    } catch (err) {
      const status = (err as { status?: number })?.status;
      // Skip feedback if not authenticated yet or offline.
      if (!status || status === 401) return true; // treated as "done" (no toast)
      const name = (err as DOMException)?.name;
      if (name === "TimeoutError" || name === "AbortError") return false; // retriable
      // Permanent error — show toast immediately.
      toast.error("No se pudo guardar la apariencia", {
        description: "Revisa tu conexión o vuelve a iniciar sesión.",
        duration: 5000,
      });
      return true; // done (failed permanently)
    }
  };

  const firstAttempt = await attemptSave();
  if (!firstAttempt) {
    // One automatic retry after a brief pause for transient timeouts.
    await new Promise<void>((res) => setTimeout(res, 2000));
    const retryOk = await attemptSave();
    if (!retryOk) {
      toast.error("No se pudo guardar la apariencia", {
        description: "Revisa tu conexión o vuelve a iniciar sesión.",
        duration: 5000,
      });
    }
  }
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [mode, setModeState] = useState<ThemeMode>(() => readStoredMode());
  const [resolved, setResolved] = useState<ResolvedTheme>(() =>
    readStoredMode() === "dark"
      ? "dark"
      : readStoredMode() === "light"
        ? "light"
        : systemPrefersDark()
          ? "dark"
          : "light",
  );
  const [font, setFontState] = useState<string>(() =>
    readStoredString(FONT_STORAGE_KEY, DEFAULT_FONT),
  );
  const [accent, setAccentState] = useState<string>(() =>
    readStoredString(ACCENT_STORAGE_KEY, DEFAULT_ACCENT),
  );
  const [customAccent, setCustomAccentState] = useState<CustomAccentSpec>(() =>
    readStoredCustomAccent(),
  );
  const [density, setDensityState] = useState<DensityMode>(() => {
    const stored = readStoredString(DENSITY_STORAGE_KEY, DEFAULT_DENSITY);
    return stored === "compact" ? "compact" : DEFAULT_DENSITY;
  });

  // Refs that always hold the latest values without stale closures.
  // Used by the debounced API persist so it always sends the full current config.
  const modeRef = useRef(mode);
  const fontRef = useRef(font);
  const accentRef = useRef(accent);
  const customAccentRef = useRef(customAccent);
  const densityRef = useRef(density);
  const persistTimer = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  /** Debounced API persist — waits 600 ms after the last setter call. */
  const schedulePersist = useCallback(() => {
    if (persistTimer.current !== undefined) clearTimeout(persistTimer.current);
    persistTimer.current = setTimeout(() => {
      persistTimer.current = undefined;
      void persistAppearanceToApi({
        theme: modeRef.current,
        accent: accentRef.current,
        font: fontRef.current,
        density: densityRef.current,
        customAccentJson:
          accentRef.current === CUSTOM_ACCENT_ID
            ? JSON.stringify(customAccentRef.current)
            : null,
      });
    }, 600);
  }, []);

  // Apply font / accent / density — covers initial render and any
  // subsequent change. Custom-accent application also re-runs whenever
  // `customAccent` changes so a hue tweak previews live.
  useEffect(() => applyFont(font), [font]);
  useEffect(() => applyAccent(accent, customAccent), [accent, customAccent]);
  useEffect(() => applyDensity(density), [density]);

  // Re-enforce the dark/light class on mount and whenever `resolved` changes.
  // The index.html bootstrap script applies it synchronously before React mounts
  // to avoid FOUC, but browser extensions or other agents can strip or replace
  // the class between bootstrap and React hydration. This effect makes the
  // ThemeProvider the authoritative source of truth at runtime.
  //
  // A MutationObserver watches for external class mutations on <html> (e.g. from
  // browser extensions that inject a "light" class or reset className entirely)
  // and corrects the state immediately after, without causing React re-renders.
  useEffect(() => {
    const root = document.documentElement;

    const enforce = () => {
      const shouldBeDark = resolved === "dark";
      const hasDark = root.classList.contains("dark");
      const hasLight = root.classList.contains("light");
      if (hasLight || hasDark !== shouldBeDark) {
        root.classList.remove("light");
        applyDarkClass(resolved);
      }
    };

    // Apply immediately on mount / resolved change
    enforce();

    // Guard against re-entrant callbacks triggered by our own classList mutations
    let correcting = false;
    const observer = new MutationObserver(() => {
      if (correcting) return;
      correcting = true;
      enforce();
      correcting = false;
    });

    observer.observe(root, { attributes: true, attributeFilter: ["class"] });

    return () => observer.disconnect();
  }, [resolved]);

  // Subscribe to system preference while in "system" mode. Future OS
  // changes route through withThemeTransition so they crossfade too.
  useEffect(() => {
    if (mode !== "system") return undefined;
    const mq = window.matchMedia("(prefers-color-scheme: dark)");
    const update = () => {
      const next: ResolvedTheme = mq.matches ? "dark" : "light";
      withThemeTransition(() => {
        flushSync(() => setResolved(next));
        applyDarkClass(next);
      });
    };
    mq.addEventListener("change", update);
    return () => mq.removeEventListener("change", update);
  }, [mode]);

  const setMode = useCallback((next: ThemeMode) => {
    const nextResolved: ResolvedTheme =
      next === "dark"
        ? "dark"
        : next === "light"
          ? "light"
          : systemPrefersDark()
            ? "dark"
            : "light";

    withThemeTransition(() => {
      // flushSync ensures the new mode/resolved render lands BEFORE the
      // View Transitions API captures the new snapshot — otherwise the
      // thumb position in the new snapshot wouldn't match the new state.
      flushSync(() => {
        setModeState(next);
        setResolved(nextResolved);
      });
      applyDarkClass(nextResolved);
    });

    modeRef.current = next;
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, next);
    } catch {
      /* storage unavailable */
    }
    schedulePersist();
  }, [schedulePersist]);

  const setFont = useCallback((id: string) => {
    setFontState(id);
    fontRef.current = id;
    try {
      window.localStorage.setItem(FONT_STORAGE_KEY, id);
    } catch {
      /* storage unavailable */
    }
    schedulePersist();
  }, [schedulePersist]);

  const setAccent = useCallback((id: string) => {
    setAccentState(id);
    accentRef.current = id;
    try {
      window.localStorage.setItem(ACCENT_STORAGE_KEY, id);
    } catch {
      /* storage unavailable */
    }
    schedulePersist();
  }, [schedulePersist]);

  const setCustomAccent = useCallback((spec: CustomAccentSpec) => {
    setCustomAccentState(spec);
    customAccentRef.current = spec;
    try {
      window.localStorage.setItem(CUSTOM_ACCENT_STORAGE_KEY, JSON.stringify(spec));
    } catch {
      /* storage unavailable */
    }
    schedulePersist();
  }, [schedulePersist]);

  const setDensity = useCallback((next: DensityMode) => {
    setDensityState(next);
    densityRef.current = next;
    try {
      window.localStorage.setItem(DENSITY_STORAGE_KEY, next);
    } catch {
      /* storage unavailable */
    }
    schedulePersist();
  }, [schedulePersist]);

  /**
   * Apply all appearance values received from the API.
   * Does NOT schedule an API persist (avoids a circular write on login).
   * Does write to localStorage so the boot cache stays fresh.
   */
  const applyFromApiConfig = useCallback((config: AppearanceConfig) => {
    const theme = (config.theme === "light" || config.theme === "dark" || config.theme === "system")
      ? config.theme as ThemeMode
      : "system";
    const nextResolved: ResolvedTheme =
      theme === "dark" ? "dark"
      : theme === "light" ? "light"
      : systemPrefersDark() ? "dark" : "light";
    const newFont = config.font || DEFAULT_FONT;
    const newAccent = config.accent || DEFAULT_ACCENT;
    const newDensity = (config.density === "compact" || config.density === "comfortable")
      ? config.density as DensityMode
      : DEFAULT_DENSITY;
    let newCustomAccent: CustomAccentSpec = DEFAULT_CUSTOM_ACCENT;
    if (config.customAccentJson) {
      try {
        const parsed = JSON.parse(config.customAccentJson) as Partial<CustomAccentSpec>;
        newCustomAccent = {
          h: typeof parsed.h === "number" ? parsed.h : DEFAULT_CUSTOM_ACCENT.h,
          c: typeof parsed.c === "number" ? parsed.c : DEFAULT_CUSTOM_ACCENT.c,
        };
      } catch {
        /* malformed JSON — use default */
      }
    }

    // Apply DOM changes
    withThemeTransition(() => {
      flushSync(() => {
        setModeState(theme);
        setResolved(nextResolved);
      });
      applyDarkClass(nextResolved);
    });
    setFontState(newFont);
    setAccentState(newAccent);
    setCustomAccentState(newCustomAccent);
    setDensityState(newDensity);

    // Update refs
    modeRef.current = theme;
    fontRef.current = newFont;
    accentRef.current = newAccent;
    customAccentRef.current = newCustomAccent;
    densityRef.current = newDensity;

    // Refresh localStorage cache
    try {
      window.localStorage.setItem(THEME_STORAGE_KEY, theme);
      window.localStorage.setItem(FONT_STORAGE_KEY, newFont);
      window.localStorage.setItem(ACCENT_STORAGE_KEY, newAccent);
      window.localStorage.setItem(DENSITY_STORAGE_KEY, newDensity);
      if (config.customAccentJson) {
        window.localStorage.setItem(CUSTOM_ACCENT_STORAGE_KEY, config.customAccentJson);
      }
    } catch {
      /* storage unavailable */
    }
  }, []);

  const value = useMemo<ThemeContextValue>(
    () => ({
      mode, resolved, setMode,
      font, setFont,
      accent, setAccent,
      customAccent, setCustomAccent,
      density, setDensity,
      applyFromApiConfig,
    }),
    [
      mode, resolved, setMode,
      font, setFont,
      accent, setAccent,
      customAccent, setCustomAccent,
      density, setDensity,
      applyFromApiConfig,
    ],
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}

export function useTheme() {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useTheme must be used within ThemeProvider");
  return ctx;
}
