/**
 * MakaPivot — Syncfusion PivotViewComponent pre-configured for Maka ERP.
 *
 * - Locale: Spanish inline.
 * - Toolbar: New, Save, Export, SubTotal, GrandTotal, Formatting, FieldList.
 * - COP formatting applied to value fields via formatSettings.
 * - GroupingBar and FieldList panels enabled.
 *
 * NEVER use PivotViewComponent directly — always use MakaPivot.
 */
import {
  PivotViewComponent,
  FieldList,
  CalculatedField,
  Toolbar,
  PDFExport,
  ExcelExport,
  ConditionalFormatting,
  GroupingBar,
  DrillThrough,
  Inject,
  type ToolbarItems,
} from "@syncfusion/ej2-react-pivotview";
import { L10n } from "@syncfusion/ej2-base";
import { useTranslation } from "react-i18next";

// ── Spanish locale ────────────────────────────────────────────────────────────
L10n.load({
  es: {
    pivotview: {
      grandTotal: "Gran total",
      total: "Total",
      value: "Valor",
      noValue: "Sin valor",
      row: "Fila",
      column: "Columna",
      collapse: "Contraer",
      expand: "Expandir",
      rowAxisPrompt: "Suelta las filas aquí",
      columnAxisPrompt: "Suelta las columnas aquí",
      valueAxisPrompt: "Suelta los valores aquí",
      filterAxisPrompt: "Suelta los filtros aquí",
      filter: "Filtrar",
      filtered: "Filtrado",
      sort: "Ordenar",
      filters: "Filtros",
      rows: "Filas",
      columns: "Columnas",
      values: "Valores",
      close: "Cerrar",
      cancel: "Cancelar",
      delete: "Eliminar",
      CalculatedField: "Campo calculado",
      createCalculatedField: "Crear campo calculado",
      fieldName: "Ingresa el nombre del campo",
      error: "Error",
      invalidFormula: "Fórmula no válida",
      dropText: "Ejemplo: ('Sum(Order_Count)' + 'Sum(In_Stock)') * 250",
      dropTextMobile: "Agrega campos y edita la fórmula aquí",
      dropAction: "El campo calculado no se puede colocar en otra región excepto en el eje de valores",
      alert: "Alerta",
      warning: "Advertencia",
      ok: "Aceptar",
      search: "Buscar",
      drag: "Arrastrar",
      remove: "Eliminar",
      sum: "Suma",
      average: "Promedio",
      count: "Conteo",
      min: "Mínimo",
      max: "Máximo",
      allFields: "Todos los campos",
      formula: "Fórmula",
      addToRow: "Agregar a filas",
      addToColumn: "Agregar a columnas",
      addToValue: "Agregar a valores",
      addToFilter: "Agregar a filtros",
      emptyData: "Sin datos para mostrar",
      fieldExist: "Ya existe un campo con este nombre",
      confirmText: "Ya existe un campo de cálculo con este nombre. ¿Deseas reemplazarlo?",
      noMatches: "Sin coincidencias",
      format: "Resumir valores por",
      edit: "Editar",
      clear: "Limpiar",
      formulaField: "Arrastra y suelta campos a la fórmula",
      dragField: "Arrastra el campo a la fórmula",
      clearFilter: "Limpiar",
      by: "por",
      all: "Todos",
      multipleItems: "Múltiples elementos",
      member: "Miembro",
      label: "Etiqueta",
      date: "Fecha",
      enterValue: "Ingresa el valor",
      chooseDate: "Ingresa la fecha",
      Before: "Antes",
      BeforeOrEqualTo: "Antes o igual a",
      After: "Después",
      AfterOrEqualTo: "Después o igual a",
      labelTextContent: "Mostrar los elementos para los que la etiqueta",
      dateTextContent: "Mostrar los elementos para los que la fecha",
      valueTextContent: "Mostrar los elementos para los que",
      Equals: "Es igual a",
      DoesNotEquals: "No es igual a",
      BeginWith: "Empieza con",
      DoesNotBeginWith: "No empieza con",
      EndsWith: "Termina con",
      DoesNotEndsWith: "No termina con",
      Contains: "Contiene",
      DoesNotContains: "No contiene",
      GreaterThan: "Es mayor que",
      GreaterThanOrEqualTo: "Es mayor o igual a",
      LessThan: "Es menor que",
      LessThanOrEqualTo: "Es menor o igual a",
      Between: "Entre",
      NotBetween: "No entre",
      And: "y",
      Sum: "Suma",
      Count: "Conteo",
      DistinctCount: "Conteo único",
      Product: "Producto",
      Avg: "Promedio",
      Min: "Mínimo",
      SampleVar: "Varianza muestral",
      PopulationVar: "Varianza poblacional",
      RunningTotals: "Totales acumulados",
      Max: "Máximo",
      Index: "Índice",
      SampleStDev: "Desviación estándar muestral",
      PopulationStDev: "Desviación estándar poblacional",
      PercentageOfRowTotal: "% del total de fila",
      PercentageOfParentTotal: "% del total del padre",
      PercentageOfParentColumnTotal: "% del total de columna del padre",
      PercentageOfParentRowTotal: "% del total de fila del padre",
      DifferenceFrom: "Diferencia de",
      PercentageOfDifferenceFrom: "% de diferencia de",
      PercentageOfGrandTotal: "% del gran total",
      PercentageOfColumnTotal: "% del total de columna",
      NotEquals: "No es igual a",
      AllValues: "Todos los valores",
      conditionalFormating: "Formato condicional",
      apply: "Aplicar",
      condition: "Agregar condición",
      formatLabel: "Formato",
      valueFieldSettings: "Configuración del campo de valor",
      baseField: "Campo base:",
      baseItem: "Elemento base:",
      summarizeValuesBy: "Resumir valores por:",
      sourceName: "Nombre del campo:",
      sourceCaption: "Título del campo:",
      example: "p.ej.:",
      editorDataLimitMsg: " más elementos. Busca para refinar más.",
      deferLayoutUpdate: "Aplazar actualización del diseño",
      null: "nulo",
      undefined: "indefinido",
      grouping: "Agrupación",
      addNewGroupingField: "Agregar nuevo campo de agrupación",
      captionName: "Título",
      selectedItems: "Elementos seleccionados",
      groups: "Grupos",
      unGroupButton: "Desagrupar",
      autoGroupCaption: "Auto",
      group: "Agrupar",
      numberFormatString: "Ejemplo: C, P, 0000 %, ###0.##0#, etc.",
      stackingbar100: "Barra apilada al 100%",
      stackingcolumn100: "Columna apilada al 100%",
      stackingline100: "Línea apilada al 100%",
      stackingarea100: "Área apilada al 100%",
      stackingbar: "Barra apilada",
      stackingcolumn: "Columna apilada",
      stackingline: "Línea apilada",
      stackingarea: "Área apilada",
      plotType: "Tipo de gráfico",
      accumulationChartType: "Tipo de gráfico de acumulación",
      showSubTotals: "Mostrar subtotales",
      doNotShowSubTotals: "No mostrar subtotales",
      showSubTotalsRowsOnly: "Solo filas",
      showSubTotalsColumnsOnly: "Solo columnas",
      showGrandTotals: "Mostrar gran total",
      doNotShowGrandTotals: "No mostrar gran total",
      showGrandTotalsRowsOnly: "Solo filas",
      showGrandTotalsColumnsOnly: "Solo columnas",
      fieldList: "Mostrar lista de campos",
      grid: "Mostrar tabla",
      chart: "Mostrar gráfico",
      excelExport: "Exportar a Excel",
      pdfExport: "Exportar a PDF",
      csvExport: "Exportar a CSV",
      numberFormating: "Formato de número",
      save: "Guardar reporte",
      new: "Nuevo reporte",
      rename: "Renombrar reporte actual",
      deleteReport: "Eliminar reporte actual",
      mdxQuery: "Consulta MDX",
      toolbarFormatting: "Formato condicional",
      SubTotal: "Subtotal",
      GrandTotal: "Gran total",
      FieldList: "Lista de campos",
      Export: "Exportar",
      mdxQueryTool: "Herramienta MDX",
      buttonTooltipDropDown: "Haz clic para ver más opciones",
      grandTotalPosition: "Posición del gran total",
      Top: "Arriba",
      Bottom: "Abajo",
    },
    pivotfieldlist: {
      staticFieldList: "Lista de campos del pivote",
      fieldList: "Lista de campos",
      dropFilterPrompt: "Suelta el filtro aquí",
      dropColPrompt: "Suelta la columna aquí",
      dropRowPrompt: "Suelta la fila aquí",
      dropValPrompt: "Suelta el valor aquí",
      addPrompt: "Agrega el campo aquí",
      adaptiveFieldHeader: "Elige el campo",
      centerHeader: "Arrastra campos entre ejes:",
      add: "agregar",
      drag: "Arrastrar",
      filter: "Filtrar",
      filtered: "Filtrado",
      sort: "Ordenar",
      remove: "Eliminar",
      filters: "Filtros",
      rows: "Filas",
      columns: "Columnas",
      values: "Valores",
      CalculatedField: "Campo calculado",
      createCalculatedField: "Crear campo calculado",
      fieldName: "Ingresa el nombre del campo",
      error: "Error",
      invalidFormula: "Fórmula no válida",
      dropText: "Ejemplo: ('Sum(Order_Count)' + 'Sum(In_Stock)') * 250",
      dropTextMobile: "Agrega campos y edita la fórmula aquí",
      dropAction: "El campo calculado no se puede colocar fuera del eje de valores",
      search: "Buscar",
      close: "Cerrar",
      cancel: "Cancelar",
      ok: "Aceptar",
      allFields: "Todos los campos",
      formula: "Fórmula",
      fieldExist: "Ya existe un campo con este nombre",
      confirmText: "Ya existe un campo de cálculo con este nombre. ¿Deseas reemplazarlo?",
      noMatches: "Sin coincidencias",
      format: "Resumir valores por",
      edit: "Editar",
      clear: "Limpiar",
      formulaField: "Arrastra y suelta campos a la fórmula",
      dragField: "Arrastra el campo a la fórmula",
    },
  },
});

// ── Types ─────────────────────────────────────────────────────────────────────

export interface MakaPivotProps {
  /** Raw data rows */
  dataSource: object[];
  /** Row dimension field names */
  rows: string[];
  /** Column dimension field names */
  columns: string[];
  /** Measure field names (shown in values area) */
  values: string[];
  /** Container height; default "500px" */
  height?: number | string;
  /** When true, value fields are formatted as COP currency */
  formatAsCOP?: boolean;
}

// ── Component ─────────────────────────────────────────────────────────────────

export function MakaPivot({
  dataSource,
  rows,
  columns,
  values,
  height = 500,
  formatAsCOP = true,
}: MakaPivotProps) {
  const { i18n } = useTranslation();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const toFields = (names: string[]): any[] => names.map((n) => ({ name: n }));
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const toValueFields = (names: string[]): any[] =>
    names.map((n) => ({ name: n, caption: n }));

  const formatSettings = formatAsCOP
    ? values.map((v) => ({ name: v, format: "C0", currency: "COP", useGrouping: true }))
    : [];

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const dataSourceSettings: any = {
    dataSource,
    rows: toFields(rows),
    columns: toFields(columns),
    values: toValueFields(values),
    formatSettings,
  };

  const toolbar: ToolbarItems[] = [
    "New",
    "Save",
    "Export",
    "SubTotal",
    "GrandTotal",
    "Formatting",
    "FieldList",
  ] as ToolbarItems[];

  return (
    <PivotViewComponent
      dataSourceSettings={dataSourceSettings}
      height={height}
      width="100%"
      locale={i18n.language}
      showToolbar
      toolbar={toolbar}
      showFieldList
      showGroupingBar
      allowExcelExport
      allowPdfExport
      allowCalculatedField
      allowConditionalFormatting
      allowDrillThrough
    >
      <Inject
        services={[
          FieldList,
          CalculatedField,
          Toolbar,
          PDFExport,
          ExcelExport,
          ConditionalFormatting,
          GroupingBar,
          DrillThrough,
        ]}
      />
    </PivotViewComponent>
  );
}
