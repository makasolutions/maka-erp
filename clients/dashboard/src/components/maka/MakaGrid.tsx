/**
 * MakaGrid — Syncfusion GridComponent pre-configured for Maka ERP.
 *
 * - Locale: Spanish (es) inline — no external JSON needed.
 * - Pagination: default 20 rows, options [10, 20, 50, 100].
 * - Toolbar: Search, Excel export, PDF export.
 * - Columns are passed as ColumnModel[] so callers control the schema.
 * - Generic T keeps row-click callbacks fully typed.
 * - isLoading overlays a spinner without unmounting the grid.
 *
 * NEVER use GridComponent directly in pages — always use MakaGrid.
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
  Inject,
  type ColumnModel,
  type ExcelExportProperties,
  type PdfExportProperties,
  type ToolbarItems,
} from "@syncfusion/ej2-react-grids";
import { L10n } from "@syncfusion/ej2-base";
import { useRef } from "react";
import { useTranslation } from "react-i18next";

// ── Locale strings for the grid (ES + EN) ────────────────────────────────────
L10n.load({
  en: {
    grid: {
      EmptyRecord: "No records to display",
      GroupDropArea: "Drag a column header here to group by that column",
      UnGroup: "Click here to ungroup",
      EmptyDataSourceError: "DataSource must not be empty on initial load since columns are generated from the dataSource in AutoGenerate Column Grid",
      Add: "Add",
      Edit: "Edit",
      Cancel: "Cancel",
      Update: "Update",
      Delete: "Delete",
      Print: "Print",
      Pdfexport: "Export PDF",
      Excelexport: "Export Excel",
      Wordexport: "Export Word",
      Csvexport: "Export CSV",
      Search: "Search",
      Columnchooser: "Columns",
      Save: "Save",
      Item: "record",
      Items: "records",
      EditOperationAlert: "No records selected for edit operation",
      DeleteOperationAlert: "No records selected for delete operation",
      SaveButton: "Save",
      OKButton: "OK",
      CancelButton: "Cancel",
      EditFormTitle: "Details of ",
      AddFormTitle: "Add New Record",
      BatchSaveConfirm: "Are you sure you want to save the changes?",
      BatchSaveLostChanges: "Unsaved changes will be lost. Are you sure you want to continue?",
      ConfirmDelete: "Are you sure you want to delete this record?",
      CancelEdit: "Are you sure you want to cancel the changes?",
      ChooseColumns: "Choose Columns",
      SearchColumns: "Search Columns",
      Matchs: "No Matches Found",
      FilterButton: "Filter",
      ClearButton: "Clear",
      StartsWith: "Starts With",
      EndsWith: "Ends With",
      Contains: "Contains",
      Equal: "Equal",
      NotEqual: "Not Equal",
      LessThan: "Less Than",
      LessThanOrEqual: "Less Than Or Equal",
      GreaterThan: "Greater Than",
      GreaterThanOrEqual: "Greater Than Or Equal",
      ChooseDate: "Choose a Date",
      EnterValue: "Enter the value",
      Copy: "Copy",
      Group: "Group by this column",
      Ungroup: "Ungroup by this column",
      autoFitAll: "Auto Fit all columns",
      autoFit: "Auto Fit this column",
      Export: "Export",
      FirstPage: "First Page",
      LastPage: "Last Page",
      PreviousPage: "Previous Page",
      NextPage: "Next Page",
      SortAscending: "Sort Ascending",
      SortDescending: "Sort Descending",
      EditRecord: "Edit Record",
      DeleteRecord: "Delete Record",
      FilterMenu: "Filter",
      SelectAll: "Select All",
      Blanks: "Blanks",
      FilterTrue: "True",
      FilterFalse: "False",
      NoResult: "No results",
      ClearFilter: "Clear Filter",
      NumberFilter: "Number Filter",
      TextFilter: "Text Filter",
      DateFilter: "Date Filter",
      DateTimeFilter: "DateTime Filter",
      MatchCase: "Match Case",
      Between: "Between",
      CustomFilter: "Custom Filter",
      CustomFilterPlaceHolder: "Enter the value",
      CustomFilterDatePlaceHolder: "Choose a date",
      AND: "AND",
      OR: "OR",
      ShowRowsWhere: "Show rows where:",
      NotStartsWith: "Does Not Start With",
      Like: "Like",
      NotEndsWith: "Does Not End With",
      NotContains: "Does Not Contain",
      IsNull: "Is Null",
      NotNull: "Is Not Null",
      IsEmpty: "Is Empty",
      IsNotEmpty: "Is Not Empty",
      AddCurrentSelection: "Add current selection to filter",
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
      EmptyDataSourceError:
        "El origen de datos no debe estar vacío en la carga inicial ya que las columnas se generan desde el origen de datos en AutoGenerate Columns Grid",
      Add: "Agregar",
      Edit: "Editar",
      Cancel: "Cancelar",
      Update: "Actualizar",
      Delete: "Eliminar",
      Print: "Imprimir",
      Pdfexport: "Exportar PDF",
      Excelexport: "Exportar Excel",
      Wordexport: "Exportar Word",
      Csvexport: "Exportar CSV",
      Search: "Buscar",
      Columnchooser: "Columnas",
      Save: "Guardar",
      Item: "registro",
      Items: "registros",
      EditOperationAlert: "No hay registros seleccionados para editar",
      DeleteOperationAlert: "No hay registros seleccionados para eliminar",
      SaveButton: "Guardar",
      OKButton: "Aceptar",
      CancelButton: "Cancelar",
      EditFormTitle: "Detalles de ",
      AddFormTitle: "Agregar nuevo registro",
      BatchSaveConfirm: "¿Guardar los cambios?",
      BatchSaveLostChanges:
        "Se perderán los cambios sin guardar. ¿Seguro que deseas continuar?",
      ConfirmDelete: "¿Seguro que deseas eliminar el registro?",
      CancelEdit: "¿Seguro que deseas cancelar los cambios?",
      ChooseColumns: "Elige columnas",
      SearchColumns: "Buscar columnas",
      Matchs: "No se encontraron coincidencias",
      FilterButton: "Filtrar",
      ClearButton: "Limpiar",
      StartsWith: "Empieza con",
      EndsWith: "Termina con",
      Contains: "Contiene",
      Equal: "Igual",
      NotEqual: "Diferente",
      LessThan: "Menor que",
      LessThanOrEqual: "Menor o igual",
      GreaterThan: "Mayor que",
      GreaterThanOrEqual: "Mayor o igual",
      ChooseDate: "Elige una fecha",
      EnterValue: "Ingresa el valor",
      Copy: "Copiar",
      Group: "Agrupar por esta columna",
      Ungroup: "Desagrupar por esta columna",
      autoFitAll: "Ajustar todas las columnas",
      autoFit: "Ajustar esta columna",
      Export: "Exportar",
      FirstPage: "Primera página",
      LastPage: "Última página",
      PreviousPage: "Página anterior",
      NextPage: "Página siguiente",
      SortAscending: "Orden ascendente",
      SortDescending: "Orden descendente",
      EditRecord: "Editar registro",
      DeleteRecord: "Eliminar registro",
      FilterMenu: "Filtro",
      SelectAll: "Seleccionar todo",
      Blanks: "En blanco",
      FilterTrue: "Verdadero",
      FilterFalse: "Falso",
      NoResult: "Sin resultados",
      ClearFilter: "Limpiar filtro",
      NumberFilter: "Filtro numérico",
      TextFilter: "Filtro de texto",
      DateFilter: "Filtro de fecha",
      DateTimeFilter: "Filtro de fecha y hora",
      MatchCase: "Coincidir mayúsculas",
      Between: "Entre",
      CustomFilter: "Filtro personalizado",
      CustomFilterPlaceHolder: "Ingresa el valor",
      CustomFilterDatePlaceHolder: "Elige una fecha",
      AND: "Y",
      OR: "O",
      ShowRowsWhere: "Mostrar filas donde:",
      NotStartsWith: "No empieza con",
      Like: "Parecido a",
      NotEndsWith: "No termina con",
      NotContains: "No contiene",
      IsNull: "Es nulo",
      NotNull: "No es nulo",
      IsEmpty: "Está vacío",
      IsNotEmpty: "No está vacío",
      AddCurrentSelection: "Agregar selección actual al filtro",
    },
    pager: {
      currentPageInfo: "{0} de {1} páginas",
      totalItemsInfo: "({0} registros)",
      totalItemInfo: "({0} registro)",
      firstPageTooltip: "Primera página",
      lastPageTooltip: "Última página",
      nextPageTooltip: "Siguiente página",
      previousPageTooltip: "Página anterior",
      nextPagerTooltip: "Ir a los siguientes elementos del paginador",
      previousPagerTooltip: "Ir a los elementos anteriores del paginador",
      pagerDropDown: "Registros por página",
      pagerAllDropDown: "Registros",
      All: "Todos",
    },
  },
});

// ── Types ─────────────────────────────────────────────────────────────────────

export interface MakaGridProps<T extends object> {
  /** Row data to display */
  dataSource: T[];
  /** Column definitions — use ColumnModel from @syncfusion/ej2-react-grids */
  columns: ColumnModel[];
  /** Shows a loading overlay when true */
  isLoading?: boolean;
  /** Base file name used for Excel / PDF exports (without extension) */
  fileName?: string;
  /** Height of the grid body; default "400px" */
  height?: number | string;
  /** Fired when the user clicks a data row */
  onRowClick?: (row: T) => void;
}

// ── Component ─────────────────────────────────────────────────────────────────

export function MakaGrid<T extends object>({
  dataSource,
  columns,
  isLoading = false,
  fileName = "maka-export",
  height = "400px",
  onRowClick,
}: MakaGridProps<T>) {
  const { i18n } = useTranslation();
  const gridRef = useRef<GridComponent>(null);

  const toolbarOptions: ToolbarItems[] = ["Search", "ExcelExport", "PdfExport"];

  function handleToolbarClick(args: { item: { id?: string } }) {
    const id = args.item.id ?? "";
    if (id.endsWith("_excelexport")) {
      const props: ExcelExportProperties = { fileName: `${fileName}.xlsx` };
      void gridRef.current?.excelExport(props);
    } else if (id.endsWith("_pdfexport")) {
      const props: PdfExportProperties = { fileName: `${fileName}.pdf` };
      void gridRef.current?.pdfExport(props);
    }
  }

  function handleRowSelected(args: { data?: T }) {
    if (onRowClick && args.data) {
      onRowClick(args.data);
    }
  }

  return (
    <div className="relative">
      {isLoading && (
        <div className="absolute inset-0 z-10 flex items-center justify-center rounded-lg bg-[var(--color-card)]/70 backdrop-blur-sm">
          <div className="h-8 w-8 animate-spin rounded-full border-2 border-[var(--color-primary)] border-t-transparent" />
        </div>
      )}
      <GridComponent
        ref={gridRef}
        dataSource={dataSource}
        height={height}
        locale={i18n.language}
        allowPaging
        allowSorting
        allowFiltering
        allowGrouping
        allowExcelExport
        allowPdfExport
        toolbar={toolbarOptions}
        filterSettings={{ type: "Menu" }}
        pageSettings={{ pageSize: 20, pageSizes: [10, 20, 50, 100] }}
        toolbarClick={handleToolbarClick}
        rowSelected={handleRowSelected}
      >
        <ColumnsDirective>
          {columns.map((col) => (
            <ColumnDirective key={col.field ?? col.headerText} {...col} />
          ))}
        </ColumnsDirective>
        <Inject services={[Page, Sort, Filter, Group, Toolbar, ExcelExport, PdfExport, Search]} />
      </GridComponent>
    </div>
  );
}
