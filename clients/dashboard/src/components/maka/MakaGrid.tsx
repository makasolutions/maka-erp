/**
 * MakaGrid v3 — Syncfusion GridComponent pre-configured for Maka ERP.
 *
 * Features
 * ─────────
 * • Split-button action column: primary action fires on left click; chevron
 *   opens full dropdown (Edit · Duplicate · Export Excel · Export PDF · extras · Delete).
 * • All actions gated by permission strings from the central P object.
 * • Column reordering (drag header), resizing (drag edge), chooser (toolbar).
 * • Grouping (drag header to the drop-area above the grid).
 * • Excel-style filter popup (checkboxes + search per column).
 * • Clipboard support (Ctrl+C copies selected rows).
 * • Pager: default 20 rows, options 20 / 50 / 100 / 1000 / Todos.
 *   Shows total records, current page indicator, "Go to page" input.
 * • Header background = accent (var --color-primary) + white foreground.
 * • Selected row = full-width accent highlight.
 * • Full dark-mode + every accent colour via maka-grid.css.
 * • Reactive to i18n language switches (es / en).
 * • Currency helper: pass format:"C0" on columns to get "$  28.900.000".
 *
 * NEVER instantiate GridComponent directly in pages — always use MakaGrid.
 */
import {
  GridComponent,
  ColumnsDirective,
  ColumnDirective,
  Page,
  Sort,
  Filter,
  Group,
  Toolbar,
  ExcelExport,
  PdfExport,
  Resize,
  Reorder,
  ColumnChooser,
  Clipboard,
  Inject,
  type ColumnModel,
  type ExcelExportProperties,
  type PdfExportProperties,
  type RecordClickEventArgs,
  type ToolbarItems,
} from "@syncfusion/ej2-react-grids";
import { L10n } from "@syncfusion/ej2-base";
import {
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
} from "react";
import { useTranslation } from "react-i18next";
import {
  ChevronDown,
  Copy,
  FileSpreadsheet,
  FileText,
  Pencil,
  Trash2,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { AuthContext } from "@/auth/auth-context";
import { useLocalization } from "@/contexts/localization-context";
import "./maka-grid.css";

// ── Constants ─────────────────────────────────────────────────────────────────

/** Maps the tenant's dateFormat token to a Syncfusion date format string. */
function syncfusionDateFormat(token: string): string {
  switch (token) {
    case "MM/DD/YYYY": return "MM/dd/yyyy";
    case "YYYY-MM-DD": return "yyyy-MM-dd";
    case "DD/MM/YYYY":
    default:           return "dd/MM/yyyy";
  }
}

/** Sentinel field name for the actions column — excluded from row-click navigation. */
const ACTIONS_FIELD = "__maka_actions__";

// ── Currency helpers ──────────────────────────────────────────────────────────

/**
 * Formats a number as Colombian pesos with the symbol FIRST: "$ 28.900.000".
 * Uses the es-CO locale, which places "$" before the value (unlike the bare
 * "es" locale that Syncfusion's C0 format falls back to → "28.900.000 COP").
 */
export function formatCOP(value: number): string {
  return new Intl.NumberFormat("es-CO", {
    style: "currency",
    currency: "COP",
    maximumFractionDigits: 0,
  }).format(value);
}

/**
 * Build a right-aligned currency column whose cells always render "$ value"
 * via a valueAccessor. Sorting / filtering still operate on the raw number.
 *
 * @example
 * columns={[ makaCurrencyColumn("price", t("products.fields.price")) ]}
 */
export function makaCurrencyColumn(
  field: string,
  headerText: string,
  extra?: Partial<ColumnModel>,
): ColumnModel {
  return {
    field,
    headerText,
    textAlign: "Right",
    width: 160,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    valueAccessor: ((f: string, data: Record<string, unknown>) => {
      const v = data?.[f];
      return typeof v === "number" ? formatCOP(v) : v;
    }) as any,
    ...extra,
  };
}

// ── Locale strings (ES + EN) ──────────────────────────────────────────────────
L10n.load({
  en: {
    grid: {
      EmptyRecord: "No records to display",
      GroupDropArea: "Drag a column header here to group by that column",
      UnGroup: "Click here to ungroup",
      EmptyDataSourceError: "DataSource must not be empty on initial load",
      Add: "Add",  Edit: "Edit", Cancel: "Cancel", Update: "Update",
      Delete: "Delete", Print: "Print",
      Pdfexport: "Export PDF", Excelexport: "Export Excel",
      Wordexport: "Export Word", Csvexport: "Export CSV",
      Search: "Search", Columnchooser: "Columns", Save: "Save",
      Item: "record", Items: "records",
      EditOperationAlert: "No records selected for edit operation",
      DeleteOperationAlert: "No records selected for delete operation",
      SaveButton: "Save", OKButton: "OK", CancelButton: "Cancel",
      EditFormTitle: "Details of ", AddFormTitle: "Add New Record",
      BatchSaveConfirm: "Save the changes?",
      BatchSaveLostChanges: "Unsaved changes will be lost. Continue?",
      ConfirmDelete: "Are you sure you want to delete this record?",
      CancelEdit: "Cancel the changes?",
      ChooseColumns: "Choose Columns", SearchColumns: "Search Columns",
      Matchs: "No Matches Found",
      FilterButton: "Filter", ClearButton: "Clear",
      StartsWith: "Starts With", EndsWith: "Ends With",
      Contains: "Contains", Equal: "Equal", NotEqual: "Not Equal",
      LessThan: "Less Than", LessThanOrEqual: "Less Than Or Equal",
      GreaterThan: "Greater Than", GreaterThanOrEqual: "Greater Than Or Equal",
      ChooseDate: "Choose a Date", EnterValue: "Enter the value",
      Copy: "Copy",
      Group: "Group by this column",
      Ungroup: "Ungroup by this column",
      GroupCaption: "drop the column here to group",
      autoFitAll: "Auto Fit all columns", autoFit: "Auto Fit this column",
      Export: "Export",
      FirstPage: "First page", LastPage: "Last page",
      PreviousPage: "Previous page", NextPage: "Next page",
      SortAscending: "Sort ascending", SortDescending: "Sort descending",
      EditRecord: "Edit record", DeleteRecord: "Delete record",
      FilterMenu: "Filter", SelectAll: "Select all",
      Blanks: "Blanks", FilterTrue: "True", FilterFalse: "False",
      NoResult: "No results", ClearFilter: "Clear filter",
      NumberFilter: "Number filter", TextFilter: "Text filter",
      DateFilter: "Date filter", DateTimeFilter: "Date-time filter",
      MatchCase: "Match case", Between: "Between",
      CustomFilter: "Custom filter",
      CustomFilterPlaceHolder: "Enter value",
      CustomFilterDatePlaceHolder: "Choose date",
      AND: "AND", OR: "OR",
      ShowRowsWhere: "Show rows where:",
      NotStartsWith: "Does not start with", Like: "Like",
      NotEndsWith: "Does not end with", NotContains: "Does not contain",
      IsNull: "Is null", NotNull: "Not null",
      IsEmpty: "Is empty", IsNotEmpty: "Not empty",
      AddCurrentSelection: "Add current selection to filter",
      SelectAllCheckbox: "Select all",
      True: "Yes", False: "No",
      SortAtoZ: "Sort A to Z",
      SortZtoA: "Sort Z to A",
      SortByOldest: "Sort oldest first",
      SortByNewest: "Sort newest first",
      SortSmallestToLargest: "Sort smallest to largest",
      SortLargestToSmallest: "Sort largest to smallest",
    },
    pager: {
      currentPageInfo: "Page {0} of {1}",
      totalItemsInfo: "{0} records",
      totalItemInfo: "{0} record",
      firstPageTooltip: "First page",
      lastPageTooltip: "Last page",
      nextPageTooltip: "Next page",
      previousPageTooltip: "Previous page",
      nextPagerTooltip: "Next pages",
      previousPagerTooltip: "Previous pages",
      pagerDropDown: "Items per page",
      pagerAllDropDown: "Items",
      All: "All",
    },
  },
  es: {
    grid: {
      EmptyRecord: "Sin registros para mostrar",
      GroupDropArea: "Arrastra una columna aquí para agrupar",
      UnGroup: "Haz clic aquí para desagrupar",
      EmptyDataSourceError: "El origen de datos no puede estar vacío en la carga inicial",
      Add: "Agregar", Edit: "Editar", Cancel: "Cancelar",
      Update: "Actualizar", Delete: "Eliminar", Print: "Imprimir",
      Pdfexport: "Exportar PDF", Excelexport: "Exportar Excel",
      Wordexport: "Exportar Word", Csvexport: "Exportar CSV",
      Search: "Buscar", Columnchooser: "Columnas", Save: "Guardar",
      Item: "registro", Items: "registros",
      EditOperationAlert: "No hay registros seleccionados para editar",
      DeleteOperationAlert: "No hay registros seleccionados para eliminar",
      SaveButton: "Guardar", OKButton: "Aceptar", CancelButton: "Cancelar",
      EditFormTitle: "Detalles de ", AddFormTitle: "Agregar nuevo registro",
      BatchSaveConfirm: "¿Guardar los cambios?",
      BatchSaveLostChanges: "Se perderán los cambios. ¿Continuar?",
      ConfirmDelete: "¿Seguro que deseas eliminar este registro?",
      CancelEdit: "¿Cancelar los cambios?",
      ChooseColumns: "Elige columnas", SearchColumns: "Buscar columnas",
      Matchs: "No se encontraron coincidencias",
      FilterButton: "Filtrar", ClearButton: "Limpiar",
      StartsWith: "Empieza con", EndsWith: "Termina con",
      Contains: "Contiene", Equal: "Igual", NotEqual: "Diferente",
      LessThan: "Menor que", LessThanOrEqual: "Menor o igual",
      GreaterThan: "Mayor que", GreaterThanOrEqual: "Mayor o igual",
      ChooseDate: "Elige una fecha", EnterValue: "Ingresa el valor",
      Copy: "Copiar",
      Group: "Agrupar por esta columna",
      Ungroup: "Desagrupar por esta columna",
      GroupCaption: "suelta la columna aquí para agrupar",
      autoFitAll: "Ajustar todas las columnas",
      autoFit: "Ajustar esta columna",
      Export: "Exportar",
      FirstPage: "Primera página", LastPage: "Última página",
      PreviousPage: "Página anterior", NextPage: "Siguiente página",
      SortAscending: "Orden ascendente", SortDescending: "Orden descendente",
      EditRecord: "Editar registro", DeleteRecord: "Eliminar registro",
      FilterMenu: "Filtro", SelectAll: "Seleccionar todo",
      Blanks: "En blanco", FilterTrue: "Verdadero", FilterFalse: "Falso",
      NoResult: "Sin resultados", ClearFilter: "Limpiar filtro",
      NumberFilter: "Filtro numérico", TextFilter: "Filtro de texto",
      DateFilter: "Filtro de fecha", DateTimeFilter: "Filtro de fecha y hora",
      MatchCase: "Coincidir mayúsculas", Between: "Entre",
      CustomFilter: "Filtro personalizado",
      CustomFilterPlaceHolder: "Ingresa el valor",
      CustomFilterDatePlaceHolder: "Elige una fecha",
      AND: "Y", OR: "O",
      ShowRowsWhere: "Mostrar filas donde:",
      NotStartsWith: "No empieza con", Like: "Similar a",
      NotEndsWith: "No termina con", NotContains: "No contiene",
      IsNull: "Es nulo", NotNull: "No es nulo",
      IsEmpty: "Está vacío", IsNotEmpty: "No está vacío",
      AddCurrentSelection: "Agregar selección al filtro",
      SelectAllCheckbox: "Seleccionar todo",
      True: "Sí", False: "No",
      SortAtoZ: "Ordenar A → Z",
      SortZtoA: "Ordenar Z → A",
      SortByOldest: "Más antiguos primero",
      SortByNewest: "Más recientes primero",
      SortSmallestToLargest: "Menor a mayor",
      SortLargestToSmallest: "Mayor a menor",
    },
    pager: {
      currentPageInfo: "Página {0} de {1}",
      totalItemsInfo: "{0} registros",
      totalItemInfo: "{0} registro",
      firstPageTooltip: "Primera página",
      lastPageTooltip: "Última página",
      nextPageTooltip: "Siguiente página",
      previousPageTooltip: "Página anterior",
      nextPagerTooltip: "Siguientes páginas",
      previousPagerTooltip: "Páginas anteriores",
      pagerDropDown: "Registros por página",
      pagerAllDropDown: "Registros",
      All: "Todos",
    },
  },
});

// ── Types ─────────────────────────────────────────────────────────────────────

/**
 * Extra module-specific action injected into the per-row Split Button dropdown.
 */
export interface MakaGridAction<T> {
  /** Unique key (React key). */
  key: string;
  /** Already-translated display label. */
  label: string;
  /** Lucide icon component. */
  icon?: LucideIcon;
  /** Permission from P.* — hides the item when user lacks it. */
  perm?: string;
  /** Called when the user selects this action. */
  onClick: (row: T) => void;
  /** Render a separator BEFORE this item. */
  dividerBefore?: boolean;
  /** Red / destructive visual style. */
  destructive?: boolean;
}

/**
 * Permission strings for the built-in MakaGrid actions.
 * Supply values from the central `P` object.
 */
export interface MakaGridPermissions {
  /** Shows the "New" toolbar button. */
  create?: string;
  /** Shows "Edit" as the Split Button primary action. */
  edit?: string;
  /** Shows "Delete" in the dropdown (destructive). */
  delete?: string;
  /** Shows "Duplicate" in the dropdown. */
  duplicate?: string;
}

export interface MakaGridProps<T extends object> {
  /** Row data to display. */
  dataSource: T[];
  /** Column definitions (ColumnModel from @syncfusion/ej2-react-grids). */
  columns: ColumnModel[];
  /** Overlays a loading spinner when true. */
  isLoading?: boolean;
  /** Base filename for exports (without extension). Default "maka-export". */
  fileName?: string;
  /** Grid body height. Defaults to "auto". */
  gridHeight?: number | string;
  /** Show Column Chooser button in toolbar. Default true. */
  showColumnChooser?: boolean;
  /**
   * Singular entity name used in the empty-state message
   * ("Ningún {entityName} coincide con los filtros actuales").
   * e.g. "ticket", "producto". Defaults to a generic "registro".
   */
  entityName?: string;
  /**
   * Optional page-level filter reset. Called by the empty-state "Clear filters"
   * button AFTER the grid clears its own column filters / search, so the page
   * can also reset its general filters (search box, status pills, date ranges).
   */
  onClearFilters?: () => void;

  // ── Navigation ──────────────────────────────────────────────────────────
  /** Fires when user clicks a data row (action column excluded). */
  onRowClick?: (row: T) => void;

  // ── Permissions ──────────────────────────────────────────────────────────
  permissions?: MakaGridPermissions;

  // ── Built-in handlers ────────────────────────────────────────────────────
  /** Called by the toolbar "New" button. */
  onCreate?: () => void;
  /** Called by the Split Button primary / Edit dropdown item. */
  onEdit?: (row: T) => void;
  /** Called by the Delete dropdown item. */
  onDelete?: (row: T) => void;
  /** Called by the Duplicate dropdown item. */
  onDuplicate?: (row: T) => void;

  // ── Extensibility ────────────────────────────────────────────────────────
  /**
   * Additional module-specific actions added to the row dropdown.
   * Use `dividerBefore: true` to separate sections.
   */
  extraActions?: MakaGridAction<T>[];
}

// ── Internal context ref type ─────────────────────────────────────────────────

interface ActionsCtx<T> {
  userPerms: string[];
  permissions: MakaGridPermissions | undefined;
  onEdit: ((row: T) => void) | undefined;
  onDelete: ((row: T) => void) | undefined;
  onDuplicate: ((row: T) => void) | undefined;
  extraActions: MakaGridAction<T>[];
  tEdit: string;
  tDuplicate: string;
  tDelete: string;
  tExcelRow: string;
  tPdfRow: string;
  gridRef: React.MutableRefObject<GridComponent | null>;
  fileName: string;
}

// ── Component ─────────────────────────────────────────────────────────────────

export function MakaGrid<T extends object>({
  dataSource,
  columns,
  isLoading = false,
  fileName = "maka-export",
  gridHeight = "auto",
  showColumnChooser = true,
  entityName,
  onClearFilters,
  onRowClick,
  permissions,
  onCreate,
  onEdit,
  onDelete,
  onDuplicate,
  extraActions,
}: MakaGridProps<T>) {
  const { t, i18n } = useTranslation("common");
  const gridRef = useRef<GridComponent | null>(null);
  const authCtx = useContext(AuthContext);
  const goToLabel = t("grid.goToPage");
  const { config } = useLocalization();
  const dateFormat = syncfusionDateFormat(config.dateFormat);

  // ── Permission resolution ────────────────────────────────────────────────
  const userPerms = authCtx?.user?.permissions ?? [];
  const canCreate  = !!onCreate && (!permissions?.create || userPerms.includes(permissions.create));
  const hasActions = !!(onEdit || onDelete || onDuplicate || (extraActions && extraActions.length > 0));

  // ── Locale normalisation (es-CO → es, en-US → en) ────────────────────────
  const locale = i18n.language.startsWith("es") ? "es" : "en";

  // ── Actions context ref ───────────────────────────────────────────────────
  //
  // The template function is created ONCE (empty useMemo deps) so Syncfusion
  // receives the same function reference across renders. Values read from the
  // ref at call-time → always current.
  const actionsCtxRef = useRef<ActionsCtx<T>>({
    userPerms: [],
    permissions,
    onEdit,
    onDelete,
    onDuplicate,
    extraActions: extraActions ?? [],
    tEdit: "", tDuplicate: "", tDelete: "", tExcelRow: "", tPdfRow: "",
    gridRef,
    fileName,
  });

  // Sync ref before every render (sync, before GridComponent sees props).
  actionsCtxRef.current = {
    userPerms,
    permissions,
    onEdit,
    onDelete,
    onDuplicate,
    extraActions: extraActions ?? [],
    tEdit:     t("actions.edit"),
    tDuplicate:t("grid.duplicate"),
    tDelete:   t("actions.delete"),
    tExcelRow: t("grid.exportExcelRow"),
    tPdfRow:   t("grid.exportPdfRow"),
    gridRef,
    fileName,
  };

  // ── Split Button action column template ───────────────────────────────────
  const actionColumnTemplate = useMemo(() => {
    // eslint-disable-next-line react/display-name
    return function SplitActionCell(rowData: Record<string, unknown>) {
      const ctx = actionsCtxRef.current;
      const up  = ctx.userPerms;

      const canEdit      = !!ctx.onEdit      && (!ctx.permissions?.edit      || up.includes(ctx.permissions.edit ?? ""));
      const canDelete    = !!ctx.onDelete    && (!ctx.permissions?.delete    || up.includes(ctx.permissions.delete ?? ""));
      const canDuplicate = !!ctx.onDuplicate && (!ctx.permissions?.duplicate || up.includes(ctx.permissions.duplicate ?? ""));
      const visExtras    = ctx.extraActions.filter(a => !a.perm || up.includes(a.perm));

      if (!canEdit && !canDelete && !canDuplicate && visExtras.length === 0) return null;

      // ── Primary action (left button of the split) ──────────────────────
      // Priority: Edit → first non-destructive extra → Duplicate → Delete
      const firstExtra     = visExtras.find(a => !a.destructive);
      const PrimaryIcon    = canEdit ? Pencil : (firstExtra?.icon ?? null);
      const primaryLabel   = canEdit ? ctx.tEdit : (firstExtra?.label ?? "");
      const primaryAction  = canEdit
        ? () => ctx.onEdit?.(rowData as T)
        : firstExtra
          ? () => firstExtra.onClick(rowData as T)
          : null;

      // ── Dropdown items ─────────────────────────────────────────────────
      // Always include ALL actions in the dropdown for discoverability.
      const hasTopItems = canEdit || canDuplicate || visExtras.some(a => !a.dividerBefore);
      const hasBottomItems = canDelete || visExtras.some(a => a.dividerBefore);

      return (
        <div
          className="flex items-center justify-center"
          onClick={(e) => e.stopPropagation()}
          onMouseDown={(e) => e.stopPropagation()}
        >
          <div className="inline-flex rounded-md border border-[var(--color-border)] overflow-hidden">

            {/* Primary action button (left) */}
            {primaryAction && (
              <button
                type="button"
                title={primaryLabel}
                onClick={primaryAction}
                className={[
                  "flex h-[1.75rem] items-center gap-1 px-2",
                  "text-[0.75rem] font-medium text-[var(--color-muted-foreground)]",
                  "bg-[var(--color-card)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                  "border-r border-[var(--color-border)]",
                  "transition-colors duration-[var(--duration-fast,150ms)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--color-ring)]",
                ].join(" ")}
              >
                {PrimaryIcon && <PrimaryIcon className="size-3.5 shrink-0" />}
                <span className="hidden xl:inline">{primaryLabel}</span>
              </button>
            )}

            {/* Dropdown trigger (right arrow) */}
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <button
                  type="button"
                  aria-label={t("grid.actions")}
                  className={[
                    "flex h-[1.75rem] w-[1.625rem] items-center justify-center",
                    "bg-[var(--color-card)] hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                    "text-[var(--color-muted-foreground)]",
                    "transition-colors duration-[var(--duration-fast,150ms)]",
                    "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[var(--color-ring)]",
                  ].join(" ")}
                >
                  <ChevronDown className="size-3.5" />
                </button>
              </DropdownMenuTrigger>

              <DropdownMenuContent align="end" className="min-w-[11rem]">

                {/* Edit */}
                {canEdit && (
                  <DropdownMenuItem onClick={() => ctx.onEdit?.(rowData as T)}>
                    <Pencil className="size-3.5" />
                    {ctx.tEdit}
                  </DropdownMenuItem>
                )}

                {/* Duplicate */}
                {canDuplicate && (
                  <DropdownMenuItem onClick={() => ctx.onDuplicate?.(rowData as T)}>
                    <Copy className="size-3.5" />
                    {ctx.tDuplicate}
                  </DropdownMenuItem>
                )}

                {/* Non-divider extras */}
                {visExtras.filter(a => !a.dividerBefore).map(a => (
                  <DropdownMenuItem key={a.key} destructive={a.destructive} onClick={() => a.onClick(rowData as T)}>
                    {a.icon && <a.icon className="size-3.5" />}
                    {a.label}
                  </DropdownMenuItem>
                ))}

                {/* Export row */}
                <DropdownMenuSeparator />
                <DropdownMenuItem onClick={() => {
                  const props: ExcelExportProperties = { dataSource: [rowData], fileName: `${ctx.fileName}-row.xlsx` };
                  void ctx.gridRef.current?.excelExport(props);
                }}>
                  <FileSpreadsheet className="size-3.5" />
                  {ctx.tExcelRow}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => {
                  const props: PdfExportProperties = { dataSource: { result: [rowData], count: 1 }, fileName: `${ctx.fileName}-row.pdf` };
                  void ctx.gridRef.current?.pdfExport(props);
                }}>
                  <FileText className="size-3.5" />
                  {ctx.tPdfRow}
                </DropdownMenuItem>

                {/* Divider-before extras */}
                {visExtras.filter(a => a.dividerBefore).map(a => (
                  <span key={a.key}>
                    <DropdownMenuSeparator />
                    <DropdownMenuItem destructive={a.destructive} onClick={() => a.onClick(rowData as T)}>
                      {a.icon && <a.icon className="size-3.5" />}
                      {a.label}
                    </DropdownMenuItem>
                  </span>
                ))}

                {/* Delete (last, destructive) */}
                {canDelete && (
                  <>
                    {(hasTopItems) && <DropdownMenuSeparator />}
                    <DropdownMenuItem destructive onClick={() => ctx.onDelete?.(rowData as T)}>
                      <Trash2 className="size-3.5" />
                      {ctx.tDelete}
                    </DropdownMenuItem>
                  </>
                )}

                {void hasBottomItems /* suppress unused warning */}
              </DropdownMenuContent>
            </DropdownMenu>
          </div>
        </div>
      );
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Stable reference — reads actionsCtxRef.current at call time.

  // ── Action column definition ──────────────────────────────────────────────
  const actionColumn: ColumnModel | null = hasActions
    ? {
        field: ACTIONS_FIELD,
        headerText: t("grid.actions"),
        width: 110,
        minWidth: 88,
        maxWidth: 140,
        allowSorting: false,
        allowFiltering: false,
        allowGrouping: false,
        allowResizing: true,
        allowReordering: false,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        template: actionColumnTemplate as any,
        textAlign: "Center",
        headerTextAlign: "Center",
        customAttributes: { class: "maka-actions-cell" },
      }
    : null;

  // Inject the tenant's localized date format into any date column that
  // didn't specify its own — so cell display, the date filter UI and exports
  // all follow the user's chosen format.
  const localizedColumns: ColumnModel[] = columns.map((col) =>
    col.type === "date" && !col.format
      ? { ...col, format: dateFormat }
      : col,
  );

  const allColumns: ColumnModel[] = actionColumn ? [...localizedColumns, actionColumn] : localizedColumns;

  // ── Toolbar ───────────────────────────────────────────────────────────────
  type CustomItem = {
    text: string; tooltipText: string; prefixIcon: string;
    id: string; align: "Left" | "Right" | "Center";
  };

  const customNewBtn: CustomItem = {
    text: t("grid.newRecord"),
    tooltipText: t("grid.newRecord"),
    prefixIcon: "e-add",
    id: "maka_create_btn",
    align: "Left",
  };

  // No "Search" (pages have their own search) and no "Separator" clutter.
  // ColumnChooser is the built-in toolbar item — it anchors its popup to the
  // button automatically. Its look (eye icon + text + chevron) is themed in
  // maka-grid.css via the .e-cc-toolbar selector.
  const toolbarItems: (ToolbarItems | CustomItem)[] = [
    ...(canCreate ? [customNewBtn] : []),
    "ExcelExport" as ToolbarItems,
    "PdfExport"   as ToolbarItems,
    ...(showColumnChooser ? ["ColumnChooser" as ToolbarItems] : []),
  ];

  // ── Toolbar click handler ─────────────────────────────────────────────────
  const handleToolbarClick = useCallback(
    (args: { item?: { id?: string } }) => {
      const id = args.item?.id ?? "";
      if (id === "maka_create_btn") {
        onCreate?.();
      } else if (id.endsWith("_excelexport")) {
        const props: ExcelExportProperties = { fileName: `${fileName}.xlsx` };
        void gridRef.current?.excelExport(props);
      } else if (id.endsWith("_pdfexport")) {
        const props: PdfExportProperties = { fileName: `${fileName}.pdf` };
        void gridRef.current?.pdfExport(props);
      }
    },
    [onCreate, fileName],
  );

  // ── Row click → navigation ────────────────────────────────────────────────
  const handleRecordClick = useCallback(
    (args: RecordClickEventArgs) => {
      const clickedField = (args.column as { field?: string } | undefined)?.field;
      if (clickedField === ACTIONS_FIELD) return;
      if (onRowClick && args.rowData) onRowClick(args.rowData as T);
    },
    [onRowClick],
  );

  // ── Clear filters (empty-state button) ─────────────────────────────────────
  const clearGridFilters = useCallback(() => {
    const g = gridRef.current;
    try { g?.clearFiltering(); } catch { /* no active filters */ }
    try { g?.search(""); } catch { /* search not active */ }
    onClearFilters?.(); // let the page reset its own general filters too
  }, [onClearFilters]);

  // ── Empty-state template ───────────────────────────────────────────────────
  // Stable function reference; reads latest copy from a ref at render time so
  // the strings stay localized and the handler current without re-mounting.
  const emptyCtxRef = useRef({ title: "", message: "", clearLabel: "", onClear: clearGridFilters });
  emptyCtxRef.current = {
    title: t("grid.noRecordsTitle"),
    message: t("grid.noMatch", { item: entityName ?? t("grid.defaultEntity") }),
    clearLabel: t("grid.clearFilters"),
    onClear: clearGridFilters,
  };
  const emptyTemplate = useMemo(() => {
    // eslint-disable-next-line react/display-name
    return function EmptyState() {
      const ctx = emptyCtxRef.current;
      return (
        <div className="flex flex-col items-center justify-center gap-1.5 px-6 py-12 text-center">
          <p className="text-[14px] font-semibold text-[var(--color-foreground)]">
            {ctx.title}
          </p>
          <p className="max-w-sm text-[12.5px] leading-relaxed text-[var(--color-muted-foreground)]">
            {ctx.message}
          </p>
          <button
            type="button"
            onClick={ctx.onClear}
            className={[
              "mt-3 inline-flex h-9 items-center rounded-lg px-4 text-[13px] font-medium",
              "border border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-foreground)]",
              "hover:bg-[var(--color-accent)] transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
            ].join(" ")}
          >
            {ctx.clearLabel}
          </button>
        </div>
      );
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ── Go-to-page control — injected INTO the Syncfusion pager ────────────────
  //
  // Syncfusion has no built-in "jump to page" input, so we imperatively insert
  // one between the page-navigation buttons (.e-pagercontainer) and the
  // page-size dropdown / record count. The pager rebuilds its DOM on every
  // navigation, so we re-inject on `dataBound` (guarded against duplicates).
  const injectGoToPage = useCallback(() => {
    const grid = gridRef.current;
    if (!grid?.element) return;
    const pager = grid.element.querySelector<HTMLElement>(".e-pager");
    if (!pager) return;

    // Only meaningful with more than one page. When there's a single page,
    // remove any previously-injected control and bail — there's nowhere to go.
    const total = grid.pageSettings?.totalRecordsCount ?? 0;
    const size = grid.pageSettings?.pageSize ?? 0;
    const totalPages = size > 0 ? Math.ceil(total / size) : 1;
    if (totalPages <= 1) {
      pager.querySelector(".maka-goto")?.remove();
      return;
    }

    if (pager.querySelector(".maka-goto")) return; // already present
    const container = pager.querySelector<HTMLElement>(".e-pagercontainer");
    if (!container) return;

    const wrap = document.createElement("div");
    wrap.className = "maka-goto";

    const label = document.createElement("span");
    label.className = "maka-goto-label";
    label.textContent = goToLabel;

    const input = document.createElement("input");
    input.type = "number";
    input.min = "1";
    input.className = "maka-goto-input";
    input.placeholder = "#";

    const go = () => {
      const n = parseInt(input.value, 10);
      const totalPages = grid.pageSettings?.totalRecordsCount && grid.pageSettings.pageSize
        ? Math.ceil(grid.pageSettings.totalRecordsCount / grid.pageSettings.pageSize)
        : undefined;
      if (!Number.isNaN(n) && n >= 1 && (totalPages === undefined || n <= totalPages)) {
        grid.goToPage(n);
        input.value = "";
      }
    };
    input.addEventListener("keydown", (e) => {
      if (e.key === "Enter") { e.preventDefault(); go(); }
    });
    input.addEventListener("blur", go);

    wrap.appendChild(label);
    wrap.appendChild(input);
    container.insertAdjacentElement("afterend", wrap);
  }, [goToLabel]);

  // Re-inject when the label (language) changes after first mount.
  useEffect(() => {
    const grid = gridRef.current;
    const existing = grid?.element?.querySelector<HTMLElement>(".maka-goto .maka-goto-label");
    if (existing) existing.textContent = goToLabel;
  }, [goToLabel]);

  // ── Render ────────────────────────────────────────────────────────────────
  return (
    <div className="maka-grid-wrapper relative flex flex-col gap-0">
      {/* Loading overlay */}
      {isLoading && (
        <div
          className="absolute inset-0 z-20 flex items-center justify-center rounded-[0.75rem] bg-[var(--color-card)]/70 backdrop-blur-sm"
          aria-busy
          aria-label={t("grid.loading")}
        >
          <div className="h-8 w-8 animate-spin rounded-full border-2 border-[var(--color-primary)] border-t-transparent" />
        </div>
      )}

      <GridComponent
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        ref={gridRef as any}
        dataSource={dataSource}
        height={gridHeight}
        locale={locale}
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        emptyRecordTemplate={emptyTemplate as any}
        /* ── Features ── */
        allowPaging
        allowSorting
        allowFiltering
        allowGrouping
        allowExcelExport
        allowPdfExport
        allowResizing
        allowReordering
        showColumnChooser={showColumnChooser}
        enableAltRow
        clipMode="EllipsisWithTooltip"
        enablePersistence={false}
        /* ── Settings ── */
        filterSettings={{ type: "Excel" }}
        pageSettings={{
          pageSize: 20,
          pageSizes: [20, 50, 100, 1000, "All"],
          pageCount: 5,
        }}
        selectionSettings={{ type: "Single", mode: "Row" }}
        /* ── Toolbar ── */
        toolbar={toolbarItems as ToolbarItems[]}
        toolbarClick={handleToolbarClick}
        /* ── Interaction ── */
        recordClick={handleRecordClick}
        created={injectGoToPage}
        dataBound={injectGoToPage}
        rowDataBound={(args) => {
          if (onRowClick && args.row) {
            (args.row as HTMLElement).style.cursor = "pointer";
          }
        }}
      >
        <ColumnsDirective>
          {allColumns.map((col) => (
            <ColumnDirective
              key={col.field ?? col.headerText}
              {...col}
            />
          ))}
        </ColumnsDirective>
        <Inject
          services={[
            Page, Sort, Filter, Group, Toolbar,
            ExcelExport, PdfExport,
            Resize, Reorder, ColumnChooser, Clipboard,
          ]}
        />
      </GridComponent>
    </div>
  );
}
