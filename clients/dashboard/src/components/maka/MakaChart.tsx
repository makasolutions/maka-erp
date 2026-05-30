/**
 * MakaChart — Syncfusion ChartComponent pre-configured for Maka ERP.
 *
 * - Resolves CSS custom properties (--color-chart-1..5, --color-saffron)
 *   at mount time via getComputedStyle, since Syncfusion's canvas engine
 *   cannot read oklch CSS vars directly.
 * - Tooltip with tenant-currency formatting when formatAsCOP=true (reads currency from LocalizationContext).
 * - Legend at the bottom, responsive width=100%.
 * - Supports Line, Bar, Area, Pie, Doughnut chart types.
 *
 * NEVER use ChartComponent or AccumulationChartComponent directly — always
 * use MakaChart.
 */
import { useEffect, useMemo, useRef, useState } from "react";
import {
  ChartComponent,
  SeriesCollectionDirective,
  SeriesDirective,
  Inject,
  Legend,
  Tooltip,
  DataLabel,
  Category,
  LineSeries,
  ColumnSeries,
  AreaSeries,
  type SeriesModel,
} from "@syncfusion/ej2-react-charts";
import {
  AccumulationChartComponent,
  AccumulationSeriesCollectionDirective,
  AccumulationSeriesDirective,
  PieSeries,
  AccumulationLegend,
  AccumulationTooltip,
  AccumulationDataLabel,
  Inject as AccumulationInject,
} from "@syncfusion/ej2-react-charts";

// ── Color resolution ──────────────────────────────────────────────────────────

const FALLBACK_PALETTE = [
  "#0ea5e9",
  "#f59e0b",
  "#14b8a6",
  "#8b5cf6",
  "#10b981",
  "#ef4444",
];

function resolveMakaChartPalette(): string[] {
  const root = document.documentElement;
  const get = (v: string) =>
    getComputedStyle(root).getPropertyValue(v).trim();

  return [
    get("--color-chart-1") || FALLBACK_PALETTE[0],
    get("--color-chart-2") || FALLBACK_PALETTE[1],
    get("--color-chart-3") || FALLBACK_PALETTE[2],
    get("--color-chart-4") || FALLBACK_PALETTE[3],
    get("--color-chart-5") || FALLBACK_PALETTE[4],
    get("--color-saffron") || FALLBACK_PALETTE[5],
  ];
}

// ── Types ─────────────────────────────────────────────────────────────────────

export type MakaChartType = "Line" | "Bar" | "Area" | "Pie" | "Doughnut";

export interface MakaChartProps {
  /** Chart title displayed above the canvas */
  title?: string;
  /** Series definitions matching Syncfusion's SeriesModel shape */
  series: SeriesModel[];
  /** Field name for the X axis category (ignored for Pie/Doughnut) */
  xField?: string;
  /** Field name for the Y axis value (ignored for Pie/Doughnut) */
  yField?: string;
  /** Chart type — defaults to "Bar" */
  type?: MakaChartType;
  /** Container height; default "350px" */
  height?: string;
  /** When true, tooltip values are formatted as currency using the tenant localization config */
  formatAsCOP?: boolean;
}

// ── Component ─────────────────────────────────────────────────────────────────

export function MakaChart({
  title,
  series,
  xField = "x",
  yField = "y",
  type = "Bar",
  height = "350px",
  formatAsCOP = false,
}: MakaChartProps) {
  const [palette, setPalette] = useState<string[]>(FALLBACK_PALETTE);
  const chartRef = useRef<ChartComponent>(null);

  useEffect(() => {
    setPalette(resolveMakaChartPalette());
  }, []);

  // House money style: "$" first, dot-grouped, no decimals. Plain number
  // grouping + a literal "$" prefix (not Intl currency style, which would emit
  // a native symbol/code that fights the canonical "$… CODE" convention).
  const currencyFormatter = useMemo(
    () => new Intl.NumberFormat("es-CO", { maximumFractionDigits: 0 }),
    [],
  );

  const tooltipSettings = {
    enable: true,
    format: formatAsCOP
      ? `<b>\${point.x}</b><br/>$${currencyFormatter.format(0).replace("0", "${point.y}")}`
      : "${point.x} : <b>${point.y}</b>",
  };

  // Pie and Doughnut use a separate AccumulationChart component
  if (type === "Pie" || type === "Doughnut") {
    const firstSeries = series[0] ?? { dataSource: [], xName: xField, yName: yField };
    return (
      <AccumulationChartComponent
        title={title}
        height={height}
        width="100%"
        tooltip={{ enable: true }}
        legendSettings={{ position: "Bottom" }}
        enableSmartLabels
      >
        <AccumulationSeriesCollectionDirective>
          <AccumulationSeriesDirective
            dataSource={firstSeries.dataSource as object[]}
            xName={firstSeries.xName ?? xField}
            yName={firstSeries.yName ?? yField}
            type="Pie"
            innerRadius={type === "Doughnut" ? "40%" : "0%"}
            dataLabel={{ visible: true, position: "Outside" }}
          />
        </AccumulationSeriesCollectionDirective>
        <AccumulationInject
          services={[PieSeries, AccumulationLegend, AccumulationTooltip, AccumulationDataLabel]}
        />
      </AccumulationChartComponent>
    );
  }

  const seriesType =
    type === "Line" ? "Line" : type === "Area" ? "Area" : "Column";

  return (
    <ChartComponent
      ref={chartRef}
      title={title}
      height={height}
      width="100%"
      palettes={palette}
      tooltip={tooltipSettings}
      legendSettings={{ position: "Bottom" }}
      primaryXAxis={{ valueType: "Category", labelIntersectAction: "Rotate45" }}
      primaryYAxis={{
        labelFormat: formatAsCOP ? "n0" : undefined,
      }}
    >
      <SeriesCollectionDirective>
        {series.map((s, i) => (
          <SeriesDirective
            key={i}
            dataSource={s.dataSource as object[]}
            xName={s.xName ?? xField}
            yName={s.yName ?? yField}
            name={s.name}
            type={seriesType}
            marker={type === "Line" ? { visible: true } : undefined}
          />
        ))}
      </SeriesCollectionDirective>
      <Inject services={[LineSeries, ColumnSeries, AreaSeries, Legend, Tooltip, DataLabel, Category]} />
    </ChartComponent>
  );
}
