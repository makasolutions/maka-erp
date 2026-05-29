/**
 * MakaGrid v2 — Syncfusion GridComponent pre-configured for Maka ERP.
 *
 * What's new in v2
 * ─────────────────
 * • Action column: Radix DropdownMenu with Edit, Delete, Duplicate, per-row
 *   Excel/PDF export, and arbitrary extra actions — all gated by permissions.
 * • Virtual scrolling auto-enabled when dataSource.length > VIRTUAL_THRESHOLD.
 * • Excel-like filter popup (type: "Excel") + clipboard support.
 * • Column resizing and reordering.
 * • Column chooser (show/hide columns) in toolbar.
 * • Full dark-mode + accent-colour theme integration via CSS vars (maka-grid.css).
 * • Reactive to i18n language switches.
 * • Toolbar "New" button calls onCreate() instead of Syncfusion's built-in editor.
 * • Row-click fires onRowClick (action column excluded from navigation).
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
  Search,
  VirtualScroll,
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
import { useCallback, useContext, useMemo, useRef } from "react";
import { useTranslation } from "react-i18next";
import {
  Copy,
  FileSpreadsheet,
  FileText,
  MoreHorizontal,
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
import "./maka-grid.css";

// ── Constants ─────────────────────────────────────────────────────────────────

/** Rows above this count use pagination; at or above it, virtual scrolling. */
const VIRTUAL_THRESHOLD = 1000;

/** Field name used for the actions column — excluded from row-click navigation. */
const ACTIONS_FIELD = "__maka_actions__";

// ── Locale strings (ES + EN) ──────────────────────────────────────────────────
L10n.load({
  en: {
    grid: {
      EmptyRecord: "No records to display",
      GroupDropArea: "Drag a column header here to group",
      UnGroup: "Click here to ungroup",
      EmptyDataSourceError: "DataSource must not be empty on initial load",
      Add: "Add", Edit: "Edit", Cancel: "Cancel", Update: "Update",
      Delete: "Delete", Print: "Print",
      Pdfexport: "Export PDF", Excelexport: "Export Excel",
      Wordexport: "Export Word", Csvexport: "Export CSV",
      Search: "Search", Columnchooser: "Columns", Save: "Save",
      Item: "record", Items: "records",
      EditOperationAlert: "No records selected for edit",
      DeleteOperationAlert: "No records selected for delete",
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
      Copy: "Copy", Group: "Group by this column",
      Ungroup: "Ungroup by this column",
      autoFitAll: "Auto Fit all columns", autoFit: "Auto Fit this column",
      Export: "Export", FirstPage: "First Page", LastPage: "Last Page",
      PreviousPage: "Previous Page", NextPage: "Next Page",
      SortAscending: "Sort Ascending", SortDescending: "Sort Descending",
      EditRecord: "Edit Record", DeleteRecord: "Delete Record",
      FilterMenu: "Filter", SelectAll: "Select All", Blanks: "Blanks",
      FilterTrue: "True", FilterFalse: "False",
      NoResult: "No results", ClearFilter: "Clear Filter",
      NumberFilter: "Number Filter", TextFilter: "Text Filter",
      DateFilter: "Date Filter", DateTimeFilter: "DateTime Filter",
      MatchCase: "Match Case", Between: "Between",
      CustomFilter: "Custom Filter",
      CustomFilterPlaceHolder: "Enter the value",
      CustomFilterDatePlaceHolder: "Choose a date",
      AND: "AND", OR: "OR", ShowRowsWhere: "Show rows where:",
      NotStartsWith: "Does Not Start With", Like: "Like",
      NotEndsWith: "Does Not End With", NotContains: "Does Not Contain",
      IsNull: "Is Null", NotNull: "Is Not Null",
      IsEmpty: "Is Empty", IsNotEmpty: "Is Not Empty",
      AddCurrentSelection: "Add current selection to filter",
      SelectAllCheckbox: "Select All",
    },
    pager: {
      currentPageInfo: "{0} of {1} pages",
      totalItemsInfo: "({0} items)",
      totalItemInfo: "({0} item)",
      firstPageTooltip: "Go to first page",
      lastPageTooltip: "Go to last page",
      nextPageTooltip: "Go to next page",
      previousPageTooltip: "Go to previous page",
      nextPagerTooltip: "Go to next pager",
      previousPagerTooltip: "Go to previous pager",
      pagerDropDown: "Items per page",
      pagerAllDropDown: "Items",
      All: "All",
    },
  },
  es: {
    grid: {
      EmptyRecord: "No hay registros para mostrar",
      GroupDropArea: "Arrastra una columna aquí para agrupar",
      UnGroup: "Haz clic aquí para desagrupar",
      EmptyDataSourceError: "El origen de datos no debe estar vacío en la carga inicial",
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
      ConfirmDelete: "¿Seguro que deseas eliminar el registro?",
      CancelEdit: "¿Cancelar los cambios?",
      ChooseColumns: "Elige columnas", SearchColumns: "Buscar columnas",
      Matchs: "No se encontraron coincidencias",
      FilterButton: "Filtrar", ClearButton: "Limpiar",
      StartsWith: "Empieza con", EndsWith: "Termina con",
      Contains: "Contiene", Equal: "Igual", NotEqual: "Diferente",
      LessThan: "Menor que", LessThanOrEqual: "Menor o igual",
      GreaterThan: "Mayor que", GreaterThanOrEqual: "Mayor o igual",
      ChooseDate: "Elige una fecha", EnterValue: "Ingresa el valor",
      Copy: "Copiar", Group: "Agrupar por esta columna",
      Ungroup: "Desagrupar por esta columna",
      autoFitAll: "Ajustar todas las columnas",
      autoFit: "Ajustar esta columna",
      Export: "Exportar", FirstPage: "Primera página",
      LastPage: "Última página", PreviousPage: "Página anterior",
      NextPage: "Página siguiente",
      SortAscending: "Orden ascendente", SortDescending: "Orden descendente",
      EditRecord: "Editar registro", DeleteRecord: "Eliminar registro",
      FilterMenu: "Filtro", SelectAll: "Seleccionar todo", Blanks: "En blanco",
      FilterTrue: "Verdadero", FilterFalse: "Falso",
      NoResult: "Sin resultados", ClearFilter: "Limpiar filtro",
      NumberFilter: "Filtro numérico", TextFilter: "Filtro de texto",
      DateFilter: "Filtro de fecha", DateTimeFilter: "Filtro de fecha y hora",
      MatchCase: "Coincidir mayúsculas", Between: "Entre",
      CustomFilter: "Filtro personalizado",
      CustomFilterPlaceHolder: "Ingresa el valor",
      CustomFilterDatePlaceHolder: "Elige una fecha",
      AND: "Y", OR: "O", ShowRowsWhere: "Mostrar filas donde:",
      NotStartsWith: "No empieza con", Like: "Parecido a",
      NotEndsWith: "No termina con", NotContains: "No contiene",
      IsNull: "Es nulo", NotNull: "No es nulo",
      IsEmpty: "Está vacío", IsNotEmpty: "No está vacío",
      AddCurrentSelection: "Agregar selección actual al filtro",
      SelectAllCheckbox: "Seleccionar todo",
    },
    pager: {
      currentPageInfo: "{0} de {1} páginas",
      totalItemsInfo: "({0} registros)",
      totalItemInfo: "({0} registro)",
      firstPageTooltip: "Primera página",
      lastPageTooltip: "Última página",
      nextPageTooltip: "Siguiente página",
      previousPageTooltip: "Página anterior",
      nextPagerTooltip: "Ir a los siguientes elementos",
      previousPagerTooltip: "Ir a los anteriores elementos",
      pagerDropDown: "Registros por página",
      pagerAllDropDown: "Registros",
      All: "Todos",
    },
  },
});

// ── Types ─────────────────────────────────────────────────────────────────────

/**
 * Arbitrary action that can be injected into the per-row action menu
 * from the parent page (e.g. "View history", "Generate invoice", etc.).
 */
export interface MakaGridAction<T> {
  /** Unique key (used as React key). */
  key: string;
  /** Already-translated display label. */
  label: string;
  /** Lucide icon component. */
  icon?: LucideIcon;
  /** Permission string from P.* — hides the item when the user lacks it. */
  perm?: string;
  /** Called when the user selects this action. */
  onClick: (row: T) => void;
  /** Render a separator line before this item. */
  dividerBefore?: boolean;
  /** Red / destructive style. */
  destructive?: boolean;
}

/**
 * Permission strings for built-in MakaGrid actions.
 * Use values from the central `P` object.
 */
export interface MakaGridPermissions {
  /** Shows the "New" toolbar button. */
  create?: string;
  /** Shows Edit in the row action menu. */
  edit?: string;
  /** Shows Delete in the row action menu. */
  delete?: string;
  /** Shows Duplicate in the row action menu. */
  duplicate?: string;
}

export interface MakaGridProps<T extends object> {
  /** Row data to display. */
  dataSource: T[];
  /** Column definitions (ColumnModel from @syncfusion/ej2-react-grids). */
  columns: ColumnModel[];
  /** Overlays a loading spinner when true. */
  isLoading?: boolean;
  /** Base filename for Excel / PDF exports (without extension). */
  fileName?: string;
  /**
   * Grid body height.
   * Defaults to "auto" for <1000 rows and "600px" for ≥1000 rows.
   * Override with an explicit value if needed.
   */
  gridHeight?: number | string;
  /** Force virtual scrolling regardless of dataset size. */
  forceVirtual?: boolean;
  /** Show Column Chooser button in toolbar (default: true). */
  showColumnChooser?: boolean;

  // ── Navigation ──────────────────────────────────────────────────────────
  /** Fired when a data row is clicked (the actions column is excluded). */
  onRowClick?: (row: T) => void;

  // ── Permission guards ────────────────────────────────────────────────────
  permissions?: MakaGridPermissions;

  // ── Built-in CRUD handlers ───────────────────────────────────────────────
  /** Called by the toolbar "New" button when permissions.create is satisfied. */
  onCreate?: () => void;
  /** Called by the Edit row action when permissions.edit is satisfied. */
  onEdit?: (row: T) => void;
  /** Called by the Delete row action when permissions.delete is satisfied. */
  onDelete?: (row: T) => void;
  /** Called by the Duplicate row action when permissions.duplicate is satisfied. */
  onDuplicate?: (row: T) => void;

  // ── Extras ──────────────────────────────────────────────────────────────
  /**
   * Additional module-specific actions appended to the per-row dropdown.
   * Respect `dividerBefore` and `perm` on each action.
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
  tNew: string;
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
  gridHeight,
  forceVirtual = false,
  showColumnChooser = true,
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

  // ── Permission resolution ────────────────────────────────────────────────
  const userPerms = authCtx?.user?.permissions ?? [];

  const canCreate    = !!onCreate    && (!permissions?.create    || userPerms.includes(permissions.create));
  const hasActions   = !!(onEdit || onDelete || onDuplicate || (extraActions && extraActions.length > 0));

  // ── Virtual scrolling decision ───────────────────────────────────────────
  const useVirtual = forceVirtual || dataSource.length >= VIRTUAL_THRESHOLD;

  // When virtual, fix height; when paginated, allow auto-grow or use provided value.
  const resolvedHeight = gridHeight ?? (useVirtual ? "600px" : "auto");

  // ── Locale normalisation ─────────────────────────────────────────────────
  // i18next may return "es-CO", "es-419" etc. — normalise to the keys in L10n.
  const locale = i18n.language.startsWith("es") ? "es" : "en";

  // ── Actions context ref (stable template reads latest values) ─────────────
  //
  // The template function is created ONCE (useMemo, empty deps) so Syncfusion
  // receives the same function reference across renders, preventing unnecessary
  // grid re-initialisation. It reads from this ref at *call time*, so it always
  // has the current permissions/handlers.
  const actionsCtxRef = useRef<ActionsCtx<T>>({
    userPerms: [],
    permissions,
    onEdit,
    onDelete,
    onDuplicate,
    extraActions: extraActions ?? [],
    tNew: "",
    tEdit: "",
    tDuplicate: "",
    tDelete: "",
    tExcelRow: "",
    tPdfRow: "",
    gridRef,
    fileName,
  });

  // Sync the ref before every render (synchronous — before GridComponent sees props).
  actionsCtxRef.current = {
    userPerms,
    permissions,
    onEdit,
    onDelete,
    onDuplicate,
    extraActions: extraActions ?? [],
    tNew:        t("grid.newRecord"),
    tEdit:       t("actions.edit"),
    tDuplicate:  t("grid.duplicate"),
    tDelete:     t("actions.delete"),
    tExcelRow:   t("grid.exportExcelRow"),
    tPdfRow:     t("grid.exportPdfRow"),
    gridRef,
    fileName,
  };

  // ── Per-row actions dropdown template ─────────────────────────────────────
  const actionColumnTemplate = useMemo(() => {
    // eslint-disable-next-line react/display-name
    return function ActionCell(rowData: Record<string, unknown>) {
      const ctx = actionsCtxRef.current;
      const up  = ctx.userPerms;

      const canEdit      = !!ctx.onEdit      && (!ctx.permissions?.edit      || up.includes(ctx.permissions.edit ?? ""));
      const canDelete    = !!ctx.onDelete    && (!ctx.permissions?.delete    || up.includes(ctx.permissions.delete ?? ""));
      const canDuplicate = !!ctx.onDuplicate && (!ctx.permissions?.duplicate || up.includes(ctx.permissions.duplicate ?? ""));
      const visExtras    = ctx.extraActions.filter(a => !a.perm || up.includes(a.perm));

      if (!canEdit && !canDelete && !canDuplicate && visExtras.length === 0) return null;

      const topItems    = visExtras.filter(a => !a.dividerBefore);
      const bottomItems = visExtras.filter(a =>  a.dividerBefore);
      const hasSep1     = (canEdit || canDuplicate || topItems.length > 0) && (canDelete || bottomItems.length > 0);

      return (
        <div
          className="flex items-center justify-center"
          // Prevent row-click from firing when interacting with the dropdown.
          onClick={(e) => e.stopPropagation()}
          onMouseDown={(e) => e.stopPropagation()}
        >
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                type="button"
                aria-label={ctx.tNew}
                className={[
                  "grid h-7 w-7 cursor-pointer place-items-center rounded-md",
                  "text-[var(--color-muted-foreground)]",
                  "hover:bg-[var(--color-accent)] hover:text-[var(--color-foreground)]",
                  "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--color-ring)]",
                  "transition-colors duration-[var(--duration-fast,150ms)]",
                ].join(" ")}
              >
                <MoreHorizontal className="size-4" />
              </button>
            </DropdownMenuTrigger>

            <DropdownMenuContent align="end" className="min-w-[10rem]">
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

              {/* Module-specific extras (no divider) */}
              {topItems.map(a => (
                <DropdownMenuItem
                  key={a.key}
                  destructive={a.destructive}
                  onClick={() => a.onClick(rowData as T)}
                >
                  {a.icon && <a.icon className="size-3.5" />}
                  {a.label}
                </DropdownMenuItem>
              ))}

              {/* Export row as Excel */}
              <DropdownMenuItem
                onClick={() => {
                  const props: ExcelExportProperties = {
                    dataSource: [rowData],
                    fileName: `${ctx.fileName}-row.xlsx`,
                  };
                  void ctx.gridRef.current?.excelExport(props);
                }}
              >
                <FileSpreadsheet className="size-3.5" />
                {ctx.tExcelRow}
              </DropdownMenuItem>

              {/* Export row as PDF */}
              <DropdownMenuItem
                onClick={() => {
                  const props: PdfExportProperties = {
                    dataSource: { result: [rowData], count: 1 },
                    fileName: `${ctx.fileName}-row.pdf`,
                  };
                  void ctx.gridRef.current?.pdfExport(props);
                }}
              >
                <FileText className="size-3.5" />
                {ctx.tPdfRow}
              </DropdownMenuItem>

              {/* Separator before destructive zone */}
              {hasSep1 && <DropdownMenuSeparator />}

              {/* Module-specific extras (with divider) */}
              {bottomItems.map(a => (
                <DropdownMenuItem
                  key={a.key}
                  destructive={a.destructive}
                  onClick={() => a.onClick(rowData as T)}
                >
                  {a.icon && <a.icon className="size-3.5" />}
                  {a.label}
                </DropdownMenuItem>
              ))}

              {/* Delete (always last, destructive) */}
              {canDelete && (
                <DropdownMenuItem
                  destructive
                  onClick={() => ctx.onDelete?.(rowData as T)}
                >
                  <Trash2 className="size-3.5" />
                  {ctx.tDelete}
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      );
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []); // Stable reference — reads from actionsCtxRef.current at call time.

  // ── Action column definition ──────────────────────────────────────────────
  const actionColumn: ColumnModel | null = hasActions
    ? {
        field: ACTIONS_FIELD,
        headerText: "",
        width: 56,
        minWidth: 56,
        maxWidth: 56,
        allowSorting: false,
        allowFiltering: false,
        allowGrouping: false,
        allowResizing: false,
        allowReordering: false,
        template: actionColumnTemplate as unknown as string,
        textAlign: "Center",
        customAttributes: { class: "maka-actions-cell" },
      }
    : null;

  const allColumns: ColumnModel[] = actionColumn ? [...columns, actionColumn] : columns;

  // ── Toolbar ───────────────────────────────────────────────────────────────
  type CustomItem = { text: string; tooltipText: string; prefixIcon: string; id: string; align: "Left" | "Right" | "Center" };

  const customNewBtn: CustomItem = {
    text: t("grid.newRecord"),
    tooltipText: t("grid.newRecord"),
    prefixIcon: "e-add",
    id: "maka_create_btn",
    align: "Left",
  };

  const toolbarItems: (ToolbarItems | CustomItem)[] = [
    ...(canCreate ? [customNewBtn] : []),
    "Search" as ToolbarItems,
    "Separator" as ToolbarItems,
    "ExcelExport" as ToolbarItems,
    "PdfExport" as ToolbarItems,
    ...(showColumnChooser ? ["ColumnChooser" as ToolbarItems] : []),
  ];

  // ── Toolbar click handler ─────────────────────────────────────────────────
  const handleToolbarClick = useCallback((args: { item?: { id?: string; text?: string } }) => {
    const id = args.item?.id ?? "";
    if (id === "maka_create_btn" || id.endsWith("_add")) {
      onCreate?.();
    } else if (id.endsWith("_excelexport")) {
      const props: ExcelExportProperties = { fileName: `${fileName}.xlsx` };
      void gridRef.current?.excelExport(props);
    } else if (id.endsWith("_pdfexport")) {
      const props: PdfExportProperties = { fileName: `${fileName}.pdf` };
      void gridRef.current?.pdfExport(props);
    }
  }, [onCreate, fileName]);

  // ── Row click → navigation ────────────────────────────────────────────────
  const handleRecordClick = useCallback((args: RecordClickEventArgs) => {
    // Skip if user clicked the actions column.
    const clickedField = (args.column as { field?: string } | undefined)?.field;
    if (clickedField === ACTIONS_FIELD) return;
    if (onRowClick && args.rowData) {
      onRowClick(args.rowData as T);
    }
  }, [onRowClick]);

  // ── Services to inject ────────────────────────────────────────────────────
  // Group is incompatible with virtual scrolling.
  const services = useVirtual
    ? [Page, Sort, Filter, Toolbar, ExcelExport, PdfExport, Search, VirtualScroll, Resize, Reorder, ColumnChooser, Clipboard]
    : [Page, Sort, Filter, Group, Toolbar, ExcelExport, PdfExport, Search, Resize, Reorder, ColumnChooser, Clipboard];

  // ── Render ────────────────────────────────────────────────────────────────
  return (
    <div className="relative">
      {/* Loading overlay */}
      {isLoading && (
        <div
          className="absolute inset-0 z-20 flex items-center justify-center rounded-[0.75rem] bg-[var(--color-card)]/70 backdrop-blur-sm"
          aria-busy="true"
          aria-label={t("grid.loading")}
        >
          <div className="h-8 w-8 animate-spin rounded-full border-2 border-[var(--color-primary)] border-t-transparent" />
        </div>
      )}

      <GridComponent
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        ref={gridRef as any}
        dataSource={dataSource}
        height={resolvedHeight}
        locale={locale}
        /* ── Features ── */
        allowPaging={!useVirtual}
        allowSorting
        allowFiltering
        allowGrouping={!useVirtual}
        allowExcelExport
        allowPdfExport
        allowResizing
        allowReordering
        allowTextWrap={false}
        showColumnChooser={showColumnChooser}
        enableVirtualization={useVirtual}
        enableAltRow
        clipMode="EllipsisWithTooltip"
        enablePersistence={false}
        /* ── Settings ── */
        filterSettings={{ type: "Excel" }}
        pageSettings={{ pageSize: 20, pageSizes: [10, 20, 50, 100] }}
        selectionSettings={{ type: "Single", mode: "Row" }}
        /* ── Toolbar ── */
        toolbar={toolbarItems as ToolbarItems[]}
        toolbarClick={handleToolbarClick}
        /* ── Row interaction ── */
        recordClick={handleRecordClick}
        /* ── Row cursor hint ── */
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
        <Inject services={services} />
      </GridComponent>
    </div>
  );
}
