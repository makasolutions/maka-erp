---
name: add-translation
description: Add i18n translation keys for a new text string. Use for every piece of user-visible text added to the frontend. Never hardcode text in JSX/TSX.
argument-hint: [namespace] [key] [spanish-text] [english-text]
---

# Add i18n Translation

Every user-visible string **must** go through `t('namespace:key')`. This is a golden rule — violating it
fails the pre-commit checklist.

## Step 1 — Choose the correct namespace

| Namespace | When to use |
|---|---|
| `common` | Shared UI: Save, Cancel, Loading, errors, table labels |
| `catalog` | Products, brands, categories, SKUs |
| `inventory` | Stock, warehouses, serial numbers, movements |
| `orders` | Orders, quotes, shipping, returns |
| `crm` | Leads, opportunities, customers, activities |
| `billing` | Invoices, DIAN, taxes, credit notes |
| `logistics` | Carriers, guides, tracking, delivery |
| `warranties` | Warranty cases, serial tracking, claims |
| `imports` | Import orders, TRM, customs, freight |
| `whatsapp` | Chat, templates, agents, conversations |
| `settings` | Tenant settings, appearance, localization |
| `hr` | HR module (future) |

**Never use `common` for strings specific to a module.**

## Step 2 — Add Spanish key first

`public/locales/es/{namespace}.json`:
```json
{
  "existingSection": { "existingKey": "Valor existente" },
  "yourSection": {
    "yourKey": "Tu texto en español"
  }
}
```

## Step 3 — Add English key

`public/locales/en/{namespace}.json`:
```json
{
  "existingSection": { "existingKey": "Existing value" },
  "yourSection": {
    "yourKey": "Your text in English"
  }
}
```

**If unsure of the exact English translation**, use a reasonable approximation and add:
```json
"yourKey": "Your text in English" // TODO: review translation
```

## Step 4 — Use in component

```tsx
import { useTranslation } from "react-i18next";

function MyComponent() {
  const { t } = useTranslation("{namespace}");
  // or multiple namespaces:
  const { t } = useTranslation(["catalog", "common"]);

  return (
    <button>{t("catalog:product.save")}</button>
  );
}
```

For namespaced keys:
```tsx
t("catalog:product.save")         // namespace:key
t("common:actions.save")          // namespace:section.key
t("catalog:product.price", { currency: "COP" })  // with interpolation
```

## Step 5 — Verify

```bash
cd clients/dashboard
npm run dev
# Open http://localhost:5174
# Switch language (Settings → Language) and confirm both ES and EN render correctly
```

## Common patterns

```tsx
// Action buttons (always common)
t("common:actions.save")
t("common:actions.cancel")
t("common:actions.edit")
t("common:actions.delete")

// Status labels (always common)
t("common:status.active")
t("common:status.pending")

// Feedback messages (always common)
t("common:feedback.saving")
t("common:feedback.saved")
t("common:feedback.saveFailed")

// Table (always common)
t("common:table.noData")
t("common:table.rowsPerPage")
```

## Checklist

- [ ] Namespace is correct (not `common` for module-specific strings)
- [ ] Key added to `public/locales/es/{namespace}.json`
- [ ] Key added to `public/locales/en/{namespace}.json`
- [ ] `useTranslation` imported in the component
- [ ] `t('namespace:key')` used in JSX (no hardcoded strings)
- [ ] Tested in both ES and EN by switching language in Settings
