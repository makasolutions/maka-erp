/**
 * Maka Components Showcase — DEV ONLY
 *
 * Accessible at: http://localhost:5174/maka-components
 * DO NOT add to the sidebar navigation. Remove before production.
 *
 * Demonstrates all five Maka* Syncfusion wrappers with realistic mock
 * data drawn from Tecnoimportaciones' product catalog.
 */
import { toast } from "sonner";
import { MakaGrid, MakaChart, MakaKanban, MakaPivot, MakaScheduler, MakaAddressList, MakaPhoneList, makaCurrencyColumn } from "@/components/maka";
import type { KanbanColumn } from "@/components/maka";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import { P } from "@/auth/permissions";

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

// ── Seed catalogue ─────────────────────────────────────────────────────────
// Real Tecnoimportaciones product families + variants. Generator creates
// 1 200 rows so MakaGrid's virtual-scrolling path is exercised.

const SEED_PRODUCTS = [
  { brand: "Sony",        prefix: "SON", name: "FX3 Full-Frame Cinema Camera",       basePrice: 28900000 },
  { brand: "Sony",        prefix: "SON", name: "FX6 Full-Frame Cinema Camera",       basePrice: 32500000 },
  { brand: "Sony",        prefix: "SON", name: "FX9 Full-Frame Camera",              basePrice: 78000000 },
  { brand: "Sony",        prefix: "SON", name: "A7 IV Mirrorless",                   basePrice: 14200000 },
  { brand: "Sony",        prefix: "SON", name: "A7R V Mirrorless",                   basePrice: 24800000 },
  { brand: "Sony",        prefix: "SON", name: "A1 Mirrorless",                      basePrice: 48000000 },
  { brand: "Sony",        prefix: "SON", name: "ZV-E1 Vlog Camera",                  basePrice: 9800000  },
  { brand: "DJI",         prefix: "DJI", name: "Mavic 3 Pro Cine Premium Combo",     basePrice: 16500000 },
  { brand: "DJI",         prefix: "DJI", name: "Mavic 3 Classic",                    basePrice: 9200000  },
  { brand: "DJI",         prefix: "DJI", name: "Mini 4 Pro",                         basePrice: 4800000  },
  { brand: "DJI",         prefix: "DJI", name: "Ronin 4D 6K Combo",                  basePrice: 62000000 },
  { brand: "DJI",         prefix: "DJI", name: "RS 3 Pro Gimbal",                    basePrice: 3200000  },
  { brand: "DJI",         prefix: "DJI", name: "Inspire 3",                          basePrice: 115000000},
  { brand: "Canon",       prefix: "CAN", name: "EOS R5 C Cinema",                    basePrice: 22000000 },
  { brand: "Canon",       prefix: "CAN", name: "EOS R5 Mark II",                     basePrice: 25000000 },
  { brand: "Canon",       prefix: "CAN", name: "EOS R6 Mark II",                     basePrice: 14500000 },
  { brand: "Canon",       prefix: "CAN", name: "EOS R3",                             basePrice: 52000000 },
  { brand: "Canon",       prefix: "CAN", name: "Cinema EOS C70",                     basePrice: 36000000 },
  { brand: "Canon",       prefix: "CAN", name: "Cinema EOS C300 Mark III",           basePrice: 89000000 },
  { brand: "Nikon",       prefix: "NIK", name: "Z9 Mirrorless",                      basePrice: 26500000 },
  { brand: "Nikon",       prefix: "NIK", name: "Z8 Mirrorless",                      basePrice: 18900000 },
  { brand: "Nikon",       prefix: "NIK", name: "Z6 III",                             basePrice: 13500000 },
  { brand: "Blackmagic",  prefix: "BMD", name: "URSA Mini Pro 12K",                  basePrice: 24500000 },
  { brand: "Blackmagic",  prefix: "BMD", name: "Pocket Cinema Camera 6K G2",         basePrice: 9800000  },
  { brand: "Blackmagic",  prefix: "BMD", name: "Pocket Cinema Camera 4K",            basePrice: 4200000  },
  { brand: "Blackmagic",  prefix: "BMD", name: "Cinema Camera 6K Full Frame",        basePrice: 18500000 },
  { brand: "Nanlite",     prefix: "NAN", name: "FS-200B Bi-Color LED Panel",         basePrice: 3200000  },
  { brand: "Nanlite",     prefix: "NAN", name: "Forza 500B II Bi-Color LED",         basePrice: 5800000  },
  { brand: "Nanlite",     prefix: "NAN", name: "Pavotube II 15C RGBWW Tube",         basePrice: 1200000  },
  { brand: "Nanlite",     prefix: "NAN", name: "MixPanel 150 RGBWW LED Panel",       basePrice: 7200000  },
  { brand: "Godox",       prefix: "GOD", name: "AD600 Pro Witstro",                  basePrice: 4800000  },
  { brand: "Godox",       prefix: "GOD", name: "V1 Round Head Flash",                basePrice: 1450000  },
  { brand: "Godox",       prefix: "GOD", name: "SL200W III LED Video Light",         basePrice: 1800000  },
  { brand: "Godox",       prefix: "GOD", name: "AD300 Pro",                          basePrice: 2900000  },
  { brand: "Godox",       prefix: "GOD", name: "MF12 Macro Flash",                   basePrice: 890000   },
  { brand: "DZOFilm",     prefix: "DZO", name: "Pavo 2x Anamorphic 50mm T2.1",      basePrice: 8900000  },
  { brand: "DZOFilm",     prefix: "DZO", name: "Pictor Zoom 20-55mm T2.8 S35",      basePrice: 6500000  },
  { brand: "DZOFilm",     prefix: "DZO", name: "Vespid FF Prime 75mm T2.1",         basePrice: 7200000  },
  { brand: "DZOFilm",     prefix: "DZO", name: "Arles FF/VV 6× Prime Set",          basePrice: 62000000 },
  { brand: "DZOFilm",     prefix: "DZO", name: "Catta Ace 35-80mm T2.9 Zoom",       basePrice: 12800000 },
];

const VARIANTS = [
  { suffix: "", skuSuffix: "", priceAdj: 1.00, stockMod: 0  },
  { suffix: " — Kit con accesorios", skuSuffix: "-KIT",  priceAdj: 1.15, stockMod: -1 },
  { suffix: " — Body Only",          skuSuffix: "-BOD",  priceAdj: 0.88, stockMod:  2 },
  { suffix: " — Refurbished",        skuSuffix: "-RFB",  priceAdj: 0.72, stockMod:  1 },
  { suffix: " — Open Box",           skuSuffix: "-OBX",  priceAdj: 0.80, stockMod:  1 },
  { suffix: " — Bundle Profesional", skuSuffix: "-BND",  priceAdj: 1.25, stockMod: -2 },
  { suffix: " — Con garantía extendida", skuSuffix: "-GEX", priceAdj: 1.08, stockMod: 0 },
  { suffix: " — Demo",               skuSuffix: "-DMO",  priceAdj: 0.65, stockMod:  1 },
  { suffix: " — Edición Colombia",   skuSuffix: "-COL",  priceAdj: 1.05, stockMod:  3 },
  { suffix: " — Kit Studio Pro",     skuSuffix: "-STU",  priceAdj: 1.35, stockMod: -1 },
  { suffix: " — Con estuche",        skuSuffix: "-ESC",  priceAdj: 1.12, stockMod:  1 },
  { suffix: " — Combo Vlog",         skuSuffix: "-VLG",  priceAdj: 1.18, stockMod:  2 },
  { suffix: " — Kit Wedding",        skuSuffix: "-WED",  priceAdj: 1.22, stockMod:  0 },
  { suffix: " — Versión Cinema",     skuSuffix: "-CIN",  priceAdj: 1.30, stockMod: -2 },
  { suffix: " — Edición Limitada",   skuSuffix: "-LTD",  priceAdj: 1.45, stockMod: -3 },
  { suffix: " — Pack Viajero",       skuSuffix: "-TRV",  priceAdj: 1.10, stockMod:  1 },
  { suffix: " — Kit Broadcast",      skuSuffix: "-BRD",  priceAdj: 1.40, stockMod: -1 },
  { suffix: " — Con batería extra",  skuSuffix: "-BAT",  priceAdj: 1.09, stockMod:  2 },
  { suffix: " — Versión Blanca",     skuSuffix: "-WHT",  priceAdj: 1.03, stockMod:  1 },
  { suffix: " — Edición Especial",   skuSuffix: "-SPC",  priceAdj: 1.20, stockMod:  0 },
  { suffix: " — Pack Escolar",       skuSuffix: "-SCL",  priceAdj: 0.92, stockMod:  4 },
  { suffix: " — Kit Producción",     skuSuffix: "-PRO",  priceAdj: 1.50, stockMod: -3 },
  { suffix: " — Versión Compacta",   skuSuffix: "-CMP",  priceAdj: 0.95, stockMod:  2 },
  { suffix: " — Edición Bogotá",     skuSuffix: "-BOG",  priceAdj: 1.02, stockMod:  3 },
  { suffix: " — Super Bundle",       skuSuffix: "-SUP",  priceAdj: 1.60, stockMod: -4 },
  { suffix: " — Versión SE",         skuSuffix: "-SE",   priceAdj: 1.06, stockMod:  1 },
  { suffix: " — Combo Foto+Video",   skuSuffix: "-FV",   priceAdj: 1.28, stockMod: -1 },
  { suffix: " — Kit Deportivo",      skuSuffix: "-DEP",  priceAdj: 1.16, stockMod:  2 },
  { suffix: " — Con mochila",        skuSuffix: "-MOC",  priceAdj: 1.07, stockMod:  1 },
  { suffix: " — Versión Professional", skuSuffix: "-PRF", priceAdj: 1.38, stockMod: -2 },
];

function _generateDemoProducts(): Product[] {
  const rows: Product[] = [];
  let id = 1;
  for (const seed of SEED_PRODUCTS) {
    for (const v of VARIANTS) {
      const rawStock = Math.max(0, Math.floor(Math.random() * 15) + v.stockMod + 2);
      rows.push({
        id,
        sku: `${seed.prefix}-${String(id).padStart(4, "0")}${v.skuSuffix}`,
        name: `${seed.name}${v.suffix}`,
        brand: seed.brand,
        price: Math.round(seed.basePrice * v.priceAdj / 1000) * 1000,
        stock: rawStock,
        status: rawStock === 0 ? "Agotado" : rawStock <= 3 ? "Stock bajo" : "Activo",
      });
      id++;
    }
  }
  return rows;
}

const DEMO_PRODUCTS_1200: Product[] = _generateDemoProducts();

const GRID_COLUMNS: ColumnModel[] = [
  { field: "sku", headerText: "SKU", width: 160, isPrimaryKey: true },
  { field: "name", headerText: "Producto", minWidth: 200 },
  { field: "brand", headerText: "Marca", width: 120 },
  makaCurrencyColumn("price", "Precio COP"),
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
            dataSource={DEMO_PRODUCTS_1200}
            columns={GRID_COLUMNS}
            fileName="productos-maka"
            permissions={{
              create:    P.catalog.products.create,
              edit:      P.catalog.products.update,
              delete:    P.catalog.products.delete,
              duplicate: P.catalog.products.create,
            }}
            onCreate={() => toast.info("Demo: crear producto")}
            onEdit={(row) => toast.info(`Demo: editar ${(row as { name: string }).name}`)}
            onDelete={(row) => toast.warning(`Demo: eliminar ${(row as { name: string }).name}`)}
            onDuplicate={(row) => toast.info(`Demo: duplicar ${(row as { name: string }).name}`)}
            onRowClick={(row) => toast.info(`Demo: clic en fila — ${(row as { sku: string }).sku}`)}
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

      <section className="space-y-3">
        <SectionHeader
          number="6"
          title="MakaAddressList"
          description="Control genérico polimórfico (PR-G1). CRUD autónomo de direcciones por owner (OwnerType + OwnerId). Montaje aislado de QA contra un owner sandbox."
        />
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <MakaAddressList ownerType="DemoOwner" ownerId={DEMO_OWNER_ID} />
        </div>
      </section>

      <section className="space-y-3">
        <SectionHeader
          number="7"
          title="MakaPhoneList"
          description="Control genérico polimórfico (PR-G2). CRUD autónomo de teléfonos por owner (OwnerType + OwnerId). Montaje aislado de QA contra un owner sandbox."
        />
        <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4">
          <MakaPhoneList ownerType="DemoOwner" ownerId={DEMO_OWNER_ID} />
        </div>
      </section>
    </div>
  );
}

// Owner sandbox estable para el montaje aislado de QA del control genérico.
const DEMO_OWNER_ID = "01900000-0000-7000-8000-000000000001";

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
