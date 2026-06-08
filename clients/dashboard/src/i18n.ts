import i18next from "i18next";
import { initReactI18next } from "react-i18next";
import LanguageDetector from "i18next-browser-languagedetector";
import Backend from "i18next-http-backend";

i18next
  .use(Backend)
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    fallbackLng: "es",
    defaultNS: "common",
    ns: ["common", "catalog", "inventory", "orders", "crm", "settings", "identity", "activity", "audits", "health", "tickets", "system", "files", "chat", "lookups"],
    backend: {
      loadPath: "/locales/{{lng}}/{{ns}}.json",
    },
    detection: {
      order: ["localStorage", "navigator"],
      caches: ["localStorage"],
    },
    interpolation: {
      escapeValue: false,
    },
    react: {
      // Disable Suspense mode so language switches trigger plain re-renders
      // instead of throwing Promises.  The shell components (Topbar, Sidebar)
      // live outside the route-level Suspense boundaries, so Suspense mode
      // causes the language-change event to be silently swallowed.
      //
      // With useSuspense:false react-i18next re-renders on both events:
      //   - "languageChanged" — immediately when the language code changes
      //   - "loaded"          — once all namespace JSON files have arrived
      useSuspense: false,
      bindI18n: "languageChanged loaded",
    },
  });

export default i18next;
