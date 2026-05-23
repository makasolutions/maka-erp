/**
 * Maka Components Showcase — DEV ONLY
 *
 * Accessible at: http://localhost:5174/maka-components
 * DO NOT add to the sidebar navigation. Remove before production.
 *
 * Demonstrates all five Maka* Syncfusion wrappers with realistic mock
 * data drawn from Tecnoimportaciones' product catalog.
 */
import { MakaGrid, MakaChart, MakaKanban, MakaPivot, MakaScheduler } from "@/components/maka";
import type { KanbanColumn } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";

// ── Mock data ─────────────────────────────────────────────────────────────────

interface Product {
  id: number;
  sku: string;
  name: string;
  brand: string;
  price: number;
  stock: number;
  status: string;
}

const MOCK_PRODUCTS: Product[] = [
  { id: 1, sku: "SON-FX3-001", name: "Sony FX3 Full-Frame Cinema Camera", brand: "Sony", price: 28900000, stock: 4, status: "Activo" },
  { id: 2, sku: "DJI-MV3-PRO", name: "DJI Mavic 3 Pro Cine", brand: "DJI", price: 12500000, stock: 7, status: "Activo" },
  { id: 3, sku: "CAN-R5C-001", name: "Canon EOS R5 C Cinema", brand: "Canon", price: 22000000, stock: 3, status: "Activo" },
  { id: 4, sku: "NIK-Z9-001",  name: "Nikon Z9 Mirrorless", brand: "Nikon", price: 26500000, stock: 2, status: "Activo" },
  { id: 5, sku: "BMD-URSA-G2", name: "Blackmagic URSA Mini Pro G2", brand: "Blackmagic", price: 18700000, stock: 5, status: "Activo" },
  { id: 6, sku: "NAN-FS-200B", name: "Nanlite FS-200B Bi-Color LED", brand: "Nanlite", price: 3200000, stock: 12, status: "Activo" },
  { id: 7, sku: "GOD-AD600-P", name: "Godox AD600 Pro Witstro", brand: "Godox", price: 4800000, stock: 9, status: "Activo" },
  { id: 8, sku: "DZO-PAVO-50", name: "DZOFilm Pavo 2x Anamorphic 50mm T2.1", brand: "DZOFilm", price: 8900000, stock: 1, status: "Agotado" },
];

const GRID_COLUMNS: ColumnModel[] = [
  { field: "sku", headerText: "SKU", width: 160, isPrimaryKey: true },
  { field: "name", headerText: "Producto", minWidth: 200 },
  { field: "brand", headerText: "Marca", width: 120 },
  {
    field: "price",
    headerText: "Precio COP",
    width: 160,
    format: "C0",
    textAlign: "Right",
  },
  { field: "stock", headerText: "Stock", width: 90, textAlign: "Right" },
  { field: "status", headerText: "Estado", width: 110 },
];

// ── Monthly sales chart data (COP millions) ───────────────────────────────────

const MONTHLY_SALES = [
  { mes: "Ene", ventas: 185000000 },
  { mes: "Feb", ventas: 214000000 },
  { mes: "Mar", ventas: 198000000 },
  { mes: "Abr", ventas: 267000000 },
  { mes: "May", ventas: 312000000 },
  { mes: "Jun", ventas: 289000000 },
  { mes: "Jul", ventas: 334000000 },
  { mes: "Ago", ventas: 301000000 },
  { mes: "Sep", ventas: 358000000 },
  { mes: "Oct", ventas: 421000000 },
  { mes: "Nov", ventas: 492000000 },
  { mes: "Dic", ventas: 567000000 },
];

const CHART_SERIES = [
  {
    dataSource: MONTHLY_SALES,
    xName: "mes",
    yName: "ventas",
    name: "Ventas 2025",
  },
];

// ── Kanban pipeline data ──────────────────────────────────────────────────────

const KANBAN_COLUMNS: KanbanColumn[] = [
  { id: "nuevo",      headerText: "Nuevo",       maxCount: 8 },
  { id: "enproceso",  headerText: "En Proceso",   maxCount: 5 },
  { id: "completado", headerText: "Completado" },
];

const KANBAN_CARDS = [
  {
    id: "K001",
    title: "Sony FX3 × 2 — Maka Studios",
    description: "Pedido urgente para producción de contenido. Cliente VIP.",
    status: "nuevo",
  },
  {
    id: "K002",
    title: "DJI Mavic 3 Pro — Drone Shop",
    description: "Distribución B2B. Requiere factura exportación.",
    status: "nuevo",
  },
  {
    id: "K003",
    title: "Kit Godox AD600 Pro × 3",
    description: "Estudio fotográfico en Medellín. Sandra gestiona.",
    status: "enproceso",
  },
  {
    id: "K004",
    title: "Nanlite FS-200B × 5 — Universidad",
    description: "Licitación pública. Documentos entregados.",
    status: "enproceso",
  },
  {
    id: "K005",
    title: "DZOFilm Pavo Anamorphic 50mm",
    description: "Entregado a director de fotografía. Factura electrónica enviada.",
    status: "completado",
  },
];

// ── Pivot table data ──────────────────────────────────────────────────────────

const PIVOT_DATA = [
  { marca: "Sony",        categoria: "Cámaras",    mes: "Ene", ventas: 28900000, unidades: 1 },
  { marca: "Sony",        categoria: "Cámaras",    mes: "Feb", ventas: 57800000, unidades: 2 },
  { marca: "DJI",         categoria: "Drones",     mes: "Ene", ventas: 25000000, unidades: 2 },
  { marca: "DJI",         categoria: "Drones",     mes: "Feb", ventas: 37500000, unidades: 3 },
  { marca: "Canon",       categoria: "Cámaras",    mes: "Ene", ventas: 44000000, unidades: 2 },
  { marca: "Canon",       categoria: "Cámaras",    mes: "Feb", ventas: 22000000, unidades: 1 },
  { marca: "Nikon",       categoria: "Cámaras",    mes: "Ene", ventas: 26500000, unidades: 1 },
  { marca: "Blackmagic",  categoria: "Cámaras",    mes: "Ene", ventas: 37400000, unidades: 2 },
  { marca: "Nanlite",     categoria: "Iluminación", mes: "Ene", ventas: 16000000, unidades: 5 },
  { marca: "Nanlite",     categoria: "Iluminación", mes: "Feb", ventas: 9600000,  unidades: 3 },
  { marca: "Godox",       categoria: "Iluminación", mes: "Ene", ventas: 19200000, unidades: 4 },
  { marca: "Godox",       categoria: "Iluminación", mes: "Feb", ventas: 24000000, unidades: 5 },
  { marca: "DZOFilm",     categoria: "Óptica",     mes: "Ene", ventas: 8900000,  unidades: 1 },
  { marca: "DZOFilm",     categoria: "Óptica",     mes: "Feb", ventas: 17800000, unidades: 2 },
];

// ── Scheduler events ──────────────────────────────────────────────────────────

function bogota(y: number, mo: number, d: number, h: number, min = 0): Date {
  // Build local Date in America/Bogota (UTC-5). Good enough for dev mocks.
  return new Date(y, mo - 1, d, h, min);
}

const now = new Date();
const y = now.getFullYear();
const mo = now.getMonth() + 1;
const d = now.getDate();

const SCHEDULER_EVENTS = [
  {
    Id: 1,
    Subject: "Demo Sony FX3 — Maka Studios",
    StartTime: bogota(y, mo, d, 10, 0),
    EndTime:   bogota(y, mo, d, 11, 0),
    Location:  "Bogotá, sede principal",
  },
  {
    Id: 2,
    Subject: "Reunión B2B — Drone Shop Medellín",
    StartTime: bogota(y, mo, d + 1, 14, 0),
    EndTime:   bogota(y, mo, d + 1, 15, 30),
    Location:  "Google Meet",
  },
  {
    Id: 3,
    Subject: "Entrega kit iluminación × 5",
    StartTime: bogota(y, mo, d + 2, 9, 0),
    EndTime:   bogota(y, mo, d + 2, 10, 0),
    Location:  "Bodega Bogotá",
  },
  {
    Id: 4,
    Subject: "Revisión de inventario cíclico",
    StartTime: bogota(y, mo, d + 3, 8, 0),
    EndTime:   bogota(y, mo, d + 3, 12, 0),
    Location:  "Bodega Medellín",
  },
  {
    Id: 5,
    Subject: "Capacitación DJI — equipo comercial",
    StartTime: bogota(y, mo, d - 1, 16, 0),
    EndTime:   bogota(y, mo, d - 1, 17, 30),
    Location:  "Sala de conferencias",
  },
];

// ── Page ──────────────────────────────────────────────────────────────────────

export function MakaComponentsPage() {
  return (
    <div className="mx-auto max-w-[1400px] space-y-10 p-6">
      {/* Header */}
      <div className="space-y-1">
        <p className="text-xs font-semibold uppercase tracking-widest text-[var(--color-muted-foreground)]">
          DEV ONLY — eliminar antes de producción
        </p>
        <h1 className="text-2xl font-bold text-[var(--color-foreground)]">
          Maka Components
        </h1>
        <p className="text-sm text-[var(--color-muted-foreground)]">
          Wrappers Syncfusion pre-configurados — locale ES, COP, Bogotá.
        </p>
      </div>

      {/* ── 1. MakaGrid ──────────────────────────────────────────────────── */}
      <section className="space-y-3">
        <SectionHeader
          number="1"
          title="MakaGrid"
          description="GridComponent con paginación, ordenamiento, filtros, búsqueda y exportación Excel/PDF."
        />
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <MakaGrid
            dataSource={MOCK_PRODUCTS}
            columns={GRID_COLUMNS}
            fileName="productos-maka"
            height="380px"
          />
        </div>
      </section>

      {/* ── 2. MakaChart ─────────────────────────────────────────────────── */}
      <section className="space-y-3">
        <SectionHeader
          number="2"
          title="MakaChart"
          description="ChartComponent con paleta Maka (CSS vars), tooltip COP y leyenda al fondo."
        />
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <MakaChart
            title="Ventas mensuales 2025 (COP)"
            series={CHART_SERIES}
            xField="mes"
            yField="ventas"
            type="Bar"
            height="350px"
            formatAsCOP
          />
        </div>
      </section>

      {/* ── 3. MakaKanban ────────────────────────────────────────────────── */}
      <section className="space-y-3">
        <SectionHeader
          number="3"
          title="MakaKanban"
          description="KanbanComponent con drag & drop, límites WIP y locale ES."
        />
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <MakaKanban
            columns={KANBAN_COLUMNS}
            dataSource={KANBAN_CARDS}
            keyField="status"
            headerField="title"
          />
        </div>
      </section>

      {/* ── 4. MakaPivot ─────────────────────────────────────────────────── */}
      <section className="space-y-3">
        <SectionHeader
          number="4"
          title="MakaPivot"
          description="PivotViewComponent con FieldList, GroupingBar, Toolbar completo y formato COP."
        />
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <MakaPivot
            dataSource={PIVOT_DATA}
            rows={["marca"]}
            columns={["mes"]}
            values={["ventas", "unidades"]}
            height={480}
            formatAsCOP
          />
        </div>
      </section>

      {/* ── 5. MakaScheduler ─────────────────────────────────────────────── */}
      <section className="space-y-3">
        <SectionHeader
          number="5"
          title="MakaScheduler"
          description="ScheduleComponent con zona horaria Bogotá, formato 12h y vistas Día/Semana/Mes/Agenda."
        />
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <MakaScheduler events={SCHEDULER_EVENTS} height="520px" />
        </div>
      </section>
    </div>
  );
}

// ── Section header helper ─────────────────────────────────────────────────────

function SectionHeader({
  number,
  title,
  description,
}: {
  number: string;
  title: string;
  description: string;
}) {
  return (
    <div className="flex items-start gap-3">
      <span className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-[var(--color-primary)] text-xs font-bold text-[var(--color-primary-foreground)]">
        {number}
      </span>
      <div>
        <h2 className="text-base font-semibold text-[var(--color-foreground)]">{title}</h2>
        <p className="text-sm text-[var(--color-muted-foreground)]">{description}</p>
      </div>
    </div>
  );
}
