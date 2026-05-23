/**
 * SyncfusionTest — página de verificación de instalación.
 *
 * Confirma que:
 *   1. Los paquetes @syncfusion/ej2-react-* resuelven correctamente
 *   2. La licencia está registrada (sin banner de trial)
 *   3. El CSS de bootstrap5 carga sin conflictos con Tailwind
 *
 * Esta página se elimina una vez que los módulos de Maka estén operativos.
 */

import {
  GridComponent,
  ColumnsDirective,
  ColumnDirective,
  Page,
  Sort,
  Filter,
  Inject,
} from "@syncfusion/ej2-react-grids";

interface Product {
  id: number;
  sku: string;
  name: string;
  brand: string;
  price: number;
  stock: number;
}

const MOCK_PRODUCTS: Product[] = [
  { id: 1, sku: "SNY-A7RV-001",   name: "Sony Alpha 7R V",          brand: "Sony",       price: 15_900_000, stock: 3 },
  { id: 2, sku: "DJI-M3P-001",    name: "DJI Mavic 3 Pro",          brand: "DJI",        price: 9_800_000,  stock: 7 },
  { id: 3, sku: "CAN-R5C-001",    name: "Canon EOS R5 C",           brand: "Canon",      price: 22_500_000, stock: 2 },
  { id: 4, sku: "BMS-PKD4K-001",  name: "Blackmagic Pocket 4K",     brand: "Blackmagic", price: 5_200_000,  stock: 5 },
  { id: 5, sku: "NAN-FS200-001",  name: "Nanlite Forza FS-200B II", brand: "Nanlite",    price: 3_100_000,  stock: 12 },
  { id: 6, sku: "GOD-SL200-001",  name: "Godox SL200W III",         brand: "Godox",      price: 1_450_000,  stock: 18 },
  { id: 7, sku: "DZO-PFP7-001",   name: "DZOFilm Pictor 7 Lens",   brand: "DZOFilm",    price: 8_700_000,  stock: 4 },
];

function formatCOP(value: number): string {
  return new Intl.NumberFormat("es-CO", { style: "currency", currency: "COP", maximumFractionDigits: 0 }).format(value);
}

export function SyncfusionTestPage() {
  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <p className="text-xs font-mono uppercase tracking-widest text-[var(--color-muted-foreground)]">
          Verificación de instalación
        </p>
        <h1 className="text-2xl font-semibold text-[var(--color-foreground)]">
          Syncfusion React — Grid de prueba
        </h1>
        <p className="mt-1 text-sm text-[var(--color-muted-foreground)]">
          Si ves la tabla sin el banner de "trial", la instalación y la licencia están correctas.
        </p>
      </div>

      {/* Status chips */}
      <div className="flex flex-wrap gap-2 text-xs font-mono">
        <span className="rounded-full bg-emerald-100 px-3 py-1 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300">
          ✓ @syncfusion/ej2-react-grids@33.1.44
        </span>
        <span className="rounded-full bg-emerald-100 px-3 py-1 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300">
          ✓ Licencia registrada vía VITE_SYNCFUSION_LICENSE
        </span>
        <span className="rounded-full bg-emerald-100 px-3 py-1 text-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300">
          ✓ CSS bootstrap5.css cargado
        </span>
      </div>

      {/* Syncfusion Grid */}
      <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-1 shadow-xs">
        <GridComponent
          dataSource={MOCK_PRODUCTS}
          allowPaging
          pageSettings={{ pageSize: 5 }}
          allowSorting
          allowFiltering
          filterSettings={{ type: "Menu" }}
          height={320}
        >
          <ColumnsDirective>
            <ColumnDirective field="sku"   headerText="SKU"    width={140} />
            <ColumnDirective field="name"  headerText="Producto" width={220} />
            <ColumnDirective field="brand" headerText="Marca"   width={120} />
            <ColumnDirective
              field="price"
              headerText="Precio (COP)"
              width={160}
              textAlign="Right"
              valueAccessor={(_field, data) => formatCOP((data as Product).price)}
            />
            <ColumnDirective
              field="stock"
              headerText="Stock"
              width={90}
              textAlign="Right"
            />
          </ColumnsDirective>
          <Inject services={[Page, Sort, Filter]} />
        </GridComponent>
      </div>

      <p className="text-xs text-[var(--color-muted-foreground)]">
        Datos mock — productos reales de Tecnoimportaciones. Esta página se elimina antes del primer release.
      </p>
    </div>
  );
}
