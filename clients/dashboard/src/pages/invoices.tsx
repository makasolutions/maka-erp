import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { Eye, Receipt } from "lucide-react";
import type { ColumnModel } from "@syncfusion/ej2-react-grids";
import {
  getMyInvoices,
  type InvoiceDto,
  type InvoiceStatus,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { ApiRequestError } from "@/lib/api-client";
import {
  EntityPageHeader,
  EntityStatusBadge,
  ErrorBand,
  type EntityStatusTone,
} from "@/components/list";
import {
  MakaGridClient,
  MakaGridFilters,
  MakaFilterField,
  MakaFilterInput,
} from "@/components/maka";
import { formatDate, formatMoney } from "@/lib/list-helpers";

// ────────────────────────────────────────────────────────────────────
// Pure helpers — module scope so they're not re-allocated each render.
// ────────────────────────────────────────────────────────────────────

function formatPeriod(year: number, month: number) {
  return `${year}-${String(month).padStart(2, "0")}`;
}

function statusTone(status: InvoiceStatus): EntityStatusTone {
  switch (status) {
    case "Paid":
      return "success";
    case "Issued":
      return "info";
    case "Void":
      return "danger";
    case "Draft":
    default:
      return "default";
  }
}

type DueKind = "due" | "paid" | "none";

type InvoiceRow = {
  id: string;
  invoiceNumber: string;
  periodLabel: string;
  tenantId: string;
  amountLabel: string;
  status: InvoiceStatus;
  statusTone: EntityStatusTone;
  dueLabel: string;
  dueKind: DueKind;
};

// ── Cell templates (hook-free; read enriched row fields) ──────────────────
function InvoiceNumberCell(row: InvoiceRow) {
  return (
    <div className="min-w-0">
      <code className="block truncate font-mono text-[13px] font-medium tracking-tight text-[var(--color-foreground)]">
        {row.invoiceNumber}
      </code>
      <span className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
        {row.periodLabel}
      </span>
    </div>
  );
}
function CustomerCell(row: InvoiceRow) {
  return (
    <span title={row.tenantId} className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]">
      {row.tenantId}
    </span>
  );
}
function AmountCell(row: InvoiceRow) {
  return (
    <span className="block text-right font-display text-[14px] font-semibold tabular-nums text-[var(--color-foreground)]">
      {row.amountLabel}
    </span>
  );
}
function StatusCell(row: InvoiceRow) {
  return <EntityStatusBadge tone={row.statusTone}>{row.status}</EntityStatusBadge>;
}
function DueCell(row: InvoiceRow) {
  if (row.dueKind === "none") {
    return <span className="text-[12px] text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]">—</span>;
  }
  const color = row.dueKind === "due" ? "var(--color-warning)" : "var(--color-success)";
  return (
    <span className="text-[12px]" style={{ color }}>
      {row.dueLabel}
    </span>
  );
}

// ────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────

export function InvoicesPage() {
  const { t } = useTranslation("common");
  const [panelOpen, setPanelOpen] = useState(true);
  const [search, setSearch] = useState("");

  const query = useQuery({
    queryKey: ["billing", "invoices", "me"],
    queryFn: getMyInvoices,
    staleTime: 30_000,
  });

  const invoices = useMemo(() => query.data ?? [], [query.data]);

  const rows: InvoiceRow[] = useMemo(() => {
    const term = search.trim().toLowerCase();
    const sorted = [...invoices].sort(
      (a, b) => new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime(),
    );
    return sorted
      .filter((inv) => {
        if (!term) return true;
        return (
          inv.invoiceNumber.toLowerCase().includes(term) ||
          inv.status.toLowerCase().includes(term) ||
          formatPeriod(inv.periodYear, inv.periodMonth).includes(term)
        );
      })
      .map((inv) => mapRow(inv, t));
  }, [invoices, search, t]);

  const columns: ColumnModel[] = useMemo(
    () => [
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "invoiceNumber", headerText: t("invoices.columns.invoiceNumber"), template: InvoiceNumberCell as any, minWidth: 200 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "tenantId", headerText: t("invoices.columns.customer"), template: CustomerCell as any, minWidth: 180 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "amountLabel", headerText: t("invoices.columns.amount"), template: AmountCell as any, width: 160, textAlign: "Right", headerTextAlign: "Right", allowSorting: false },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "status", headerText: t("invoices.columns.status"), template: StatusCell as any, width: 130 },
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      { field: "dueLabel", headerText: t("invoices.columns.dueDate"), template: DueCell as any, width: 170, allowSorting: false },
    ],
    [t],
  );

  const errorMessage =
    query.error instanceof ApiRequestError
      ? query.error.problem?.detail ?? query.error.message
      : query.error
        ? t("feedback.failedToLoad", { resource: t("invoices.title") })
        : null;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Receipt}
        title={t("invoices.title")}
        total={query.data ? invoices.length : null}
        unit={t("invoices.invoice")}
        unitPlural={t("invoices.invoices")}
        description={t("invoices.description")}
      >
        <Button
          variant="outline"
          onClick={() => setPanelOpen((v) => !v)}
          aria-pressed={panelOpen}
          className="h-9 gap-1.5 rounded-lg px-4 text-[13px] font-semibold"
        >
          <Eye className="size-4" />
          {t("gridFilters.filtersTab")}
        </Button>
      </EntityPageHeader>

      <MakaGridFilters
        open={panelOpen}
        onClear={() => setSearch("")}
        filters={
          <MakaFilterField label={t("gridFilters.search")} className="grow">
            <MakaFilterInput
              value={search}
              onChange={setSearch}
              placeholder={t("invoices.searchPlaceholder")}
              ariaLabel={t("gridFilters.search")}
              className="min-w-64"
            />
          </MakaFilterField>
        }
      />

      {errorMessage && <ErrorBand message={errorMessage} />}

      <MakaGridClient<InvoiceRow>
        dataSource={rows}
        columns={columns}
        isLoading={query.isLoading}
        fileName="facturas"
        entityName={t("invoices.invoice")}
        onClearFilters={() => setSearch("")}
      />
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Mapping — enrich a DTO into a display row (called inside a t()-aware memo)
// ────────────────────────────────────────────────────────────────────

function mapRow(inv: InvoiceDto, t: (k: string, o?: Record<string, unknown>) => string): InvoiceRow {
  let dueLabel = "";
  let dueKind: DueKind = "none";
  if (inv.dueAtUtc && inv.status === "Issued") {
    dueLabel = `${t("invoices.due")} ${formatDate(inv.dueAtUtc)}`;
    dueKind = "due";
  } else if (inv.paidAtUtc && inv.status === "Paid") {
    dueLabel = `${t("invoices.paid")} ${formatDate(inv.paidAtUtc)}`;
    dueKind = "paid";
  } else if (inv.dueAtUtc) {
    dueLabel = formatDate(inv.dueAtUtc);
    dueKind = "due";
  }
  return {
    id: inv.id,
    invoiceNumber: inv.invoiceNumber,
    periodLabel: `${t("invoices.period")} ${formatPeriod(inv.periodYear, inv.periodMonth)}`,
    tenantId: inv.tenantId,
    amountLabel: formatMoney(inv.subtotalAmount, inv.currency),
    status: inv.status,
    statusTone: statusTone(inv.status),
    dueLabel,
    dueKind,
  };
}
