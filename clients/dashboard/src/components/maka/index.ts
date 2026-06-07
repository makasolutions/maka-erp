/**
 * Maka component library — Syncfusion wrappers pre-configured for:
 * - Locale: es (Spanish)
 * - Currency: COP (Colombian Peso)
 * - Timezone: America/Bogota
 * - Theme: Maka design tokens (CSS custom properties from globals.css)
 *
 * Always import from this barrel, never from the individual files directly.
 *
 * @example
 *   import { MakaGrid, MakaChart } from "@/components/maka";
 */

export { MakaGrid, MakaGridClient, MakaGridServer, makaCurrencyColumn, formatCOP } from "./MakaGrid";
export { MakaCurrencyInput } from "./MakaCurrencyInput";
export type { MakaCurrencyInputProps } from "./MakaCurrencyInput";
export { MakaPriceWithTax } from "./MakaPriceWithTax";
export type { MakaPriceWithTaxProps } from "./MakaPriceWithTax";
export { MakaPriceRangeFilter } from "./MakaPriceRangeFilter";
export type { MakaPriceRange, MakaPriceRangeFilterProps } from "./MakaPriceRangeFilter";
export { MakaDateTimeRangePicker } from "./MakaDateTimeRangePicker";
export type { MakaDateTimeRange, MakaDateTimeRangePickerProps } from "./MakaDateTimeRangePicker";
export type {
  MakaGridProps,
  MakaGridClientProps,
  MakaGridServerProps,
  MakaGridServerPaging,
  MakaGridPermissions,
  MakaGridAction,
} from "./MakaGrid";

export { MakaDateRangePicker, makaPresetRange } from "./MakaDateRangePicker";
export type { MakaDateRangePickerProps, MakaDateRange, MakaRangePreset } from "./MakaDateRangePicker";

export { MakaGridFilters, MakaFilterField, MakaFilterInput } from "./MakaGridFilters";
export type { MakaGridFiltersProps } from "./MakaGridFilters";

export { MakaChart } from "./MakaChart";
export type { MakaChartProps, MakaChartType } from "./MakaChart";

export { MakaKanban } from "./MakaKanban";
export type { MakaKanbanProps, KanbanColumn } from "./MakaKanban";

export { MakaPivot } from "./MakaPivot";
export type { MakaPivotProps } from "./MakaPivot";

export { MakaScheduler } from "./MakaScheduler";
export type { MakaSchedulerProps } from "./MakaScheduler";

export { MakaRichTextEditor } from "./MakaRichTextEditor";
export type { MakaRichTextEditorProps } from "./MakaRichTextEditor";
