import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import { useQuery } from "@tanstack/react-query";
import { Receipt } from "lucide-react";
import {
  getMyInvoices,
  type InvoiceDto,
  type InvoiceStatus,
} from "@/api/billing";
import { Button } from "@/components/ui/button";
import { ApiRequestError } from "@/lib/api-client";
import { useAuth } from "@/auth/use-auth";
import {
  EntityEmpty,
  EntityListCard,
  EntityListHeader,
  EntityListLoading,
  EntityListRow,
  EntityPageHeader,
  EntitySearch,
  EntityStatusBadge,
  ErrorBand,
  ToneIconTile,
  type EntityStatusTone,
} from "@/components/list";
import { formatDate } from "@/lib/list-helpers";

// ────────────────────────────────────────────────────────────────────
// Pure helpers — module scope so they're not re-allocated each render.
// ────────────────────────────────────────────────────────────────────

function formatMoney(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat(undefined, {
      style: "currency",
      currency,
    }).format(amount);
  } catch {
    return `${amount.toFixed(2)} ${currency}`;
  }
}

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

const DESKTOP_GRID =
  "grid-cols-[1fr_140px_120px_110px_120px] lg:grid-cols-[1fr_180px_140px_120px_140px]";

// ────────────────────────────────────────────────────────────────────
// Page
// ────────────────────────────────────────────────────────────────────

export function InvoicesPage() {
  const { t } = useTranslation("common");
  const { user } = useAuth();
  const query = useQuery({
    queryKey: ["billing", "invoices", "me"],
    queryFn: getMyInvoices,
    staleTime: 30_000,
  });

  // Tenant chip retained as a no-op consumer; the new shell drops the
  // legacy tenant breadcrumb from the header.
  void user;

  const [search, setSearch] = useState("");

  const invoices = query.data ?? [];

  const sorted = useMemo(
    () =>
      [...invoices].sort(
        (a, b) =>
          new Date(b.createdAtUtc).getTime() - new Date(a.createdAtUtc).getTime(),
      ),
    [invoices],
  );

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (term.length === 0) return sorted;
    return sorted.filter((inv) => {
      return (
        inv.invoiceNumber.toLowerCase().includes(term) ||
        inv.status.toLowerCase().includes(term) ||
        formatPeriod(inv.periodYear, inv.periodMonth).includes(term)
      );
    });
  }, [sorted, search]);

  const errorMessage =
    query.error instanceof ApiRequestError
      ? query.error.problem?.detail ?? query.error.message
      : query.error
        ? t("feedback.failedToLoad", { resource: t("invoices.title") })
        : null;

  const searchActive = search.trim().length > 0;

  return (
    <div className="space-y-4 sm:space-y-6">
      <EntityPageHeader
        icon={Receipt}
        title={t("invoices.title")}
        total={query.data?.length ?? null}
        unit={t("invoices.invoice")}
        description={t("invoices.description")}
      />

      <EntitySearch
        value={search}
        onChange={setSearch}
        placeholder={t("invoices.searchPlaceholder")}
      />

      {errorMessage && <ErrorBand message={errorMessage} />}

      {query.isLoading ? (
        <EntityListLoading desktopColumns={DESKTOP_GRID} rows={4} />
      ) : filtered.length === 0 ? (
        <EntityEmpty
          icon={Receipt}
          title={searchActive ? t("invoices.empty.searchTitle") : t("invoices.empty.title")}
          body={
            searchActive
              ? t("feedback.noResultsFor", { term: search.trim() })
              : t("invoices.empty.body")
          }
          action={
            searchActive ? (
              <Button
                variant="outline"
                onClick={() => setSearch("")}
                className="h-9 rounded-lg px-4 text-[13px]"
              >
                {t("invoices.empty.clearSearch")}
              </Button>
            ) : undefined
          }
        />
      ) : (
        <div>
          <div className="mb-3 flex items-center justify-between">
            <p className="text-[12px] font-medium text-[var(--color-muted-foreground)]">
              {filtered.length} {filtered.length === 1 ? t("invoices.invoice") : t("invoices.invoices")}{" "}
              {searchActive ? t("invoices.matched") : t("invoices.found")}
            </p>
          </div>

          {/* Mobile: card list */}
          <div className="space-y-2 md:hidden">
            {filtered.map((invoice) => (
              <MobileCard key={invoice.id} invoice={invoice} />
            ))}
          </div>

          {/* Desktop: table */}
          <EntityListCard className="hidden md:block">
            <EntityListHeader className={DESKTOP_GRID}>
              <span>{t("invoices.columns.invoiceNumber")}</span>
              <span>{t("invoices.columns.customer")}</span>
              <span className="text-right">{t("invoices.columns.amount")}</span>
              <span>{t("invoices.columns.status")}</span>
              <span>{t("invoices.columns.dueDate")}</span>
            </EntityListHeader>
            {filtered.map((invoice, i) => (
              <DesktopRow
                key={invoice.id}
                invoice={invoice}
                isLast={i === filtered.length - 1}
              />
            ))}
          </EntityListCard>
        </div>
      )}
    </div>
  );
}

// ────────────────────────────────────────────────────────────────────
// Subcomponents
// ────────────────────────────────────────────────────────────────────

function MobileCard({ invoice }: { invoice: InvoiceDto }) {
  const { t } = useTranslation("common");
  return (
    <div className="rounded-xl border border-[var(--color-border)] bg-[var(--color-card)] p-4 shadow-xs">
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-center gap-3">
          <ToneIconTile icon={Receipt} tone="primary" size="md" className="rounded-xl" />
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <code className="font-mono text-[12.5px] font-medium tracking-tight text-[var(--color-foreground)]">
                {invoice.invoiceNumber}
              </code>
              <EntityStatusBadge tone={statusTone(invoice.status)}>
                {invoice.status}
              </EntityStatusBadge>
            </div>
            <p className="mt-0.5 font-mono text-[11px] text-[var(--color-muted-foreground)]">
              {t("invoices.period")} {formatPeriod(invoice.periodYear, invoice.periodMonth)}
            </p>
          </div>
        </div>
        <div className="text-right">
          <div className="font-display text-[15px] font-semibold tabular-nums">
            {formatMoney(invoice.subtotalAmount, invoice.currency)}
          </div>
          {invoice.dueAtUtc && invoice.status === "Issued" && (
            <div className="mt-0.5 font-mono text-[10.5px] text-[var(--color-warning)]">
              {t("invoices.due")} {formatDate(invoice.dueAtUtc)}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

function DesktopRow({
  invoice,
  isLast,
}: {
  invoice: InvoiceDto;
  isLast: boolean;
}) {
  const { t } = useTranslation("common");
  return (
    <EntityListRow className={DESKTOP_GRID} isLast={isLast}>
      {/* Invoice number + icon */}
      <div className="flex min-w-0 items-center gap-3">
        <ToneIconTile icon={Receipt} tone="primary" size="md" className="rounded-xl" />
        <div className="min-w-0">
          <code className="block truncate font-mono text-[13px] font-medium tracking-tight text-[var(--color-foreground)]">
            {invoice.invoiceNumber}
          </code>
          <span className="mt-0.5 block truncate font-mono text-[11px] text-[var(--color-muted-foreground)]">
            {t("invoices.period")} {formatPeriod(invoice.periodYear, invoice.periodMonth)}
          </span>
        </div>
      </div>

      {/* Customer — we surface the tenant id since these are tenant-scoped invoices. */}
      <span
        title={invoice.tenantId}
        className="truncate font-mono text-[12px] text-[var(--color-muted-foreground)]"
      >
        {invoice.tenantId}
      </span>

      {/* Amount */}
      <span className="text-right font-display text-[14px] font-semibold tabular-nums">
        {formatMoney(invoice.subtotalAmount, invoice.currency)}
      </span>

      {/* Status */}
      <span>
        <EntityStatusBadge tone={statusTone(invoice.status)}>
          {invoice.status}
        </EntityStatusBadge>
      </span>

      {/* Due date */}
      <span className="text-[12px] text-[var(--color-muted-foreground)]">
        {invoice.dueAtUtc ? (
          invoice.status === "Issued" ? (
            <span className="text-[var(--color-warning)]">{formatDate(invoice.dueAtUtc)}</span>
          ) : (
            formatDate(invoice.dueAtUtc)
          )
        ) : invoice.paidAtUtc && invoice.status === "Paid" ? (
          <span className="text-[var(--color-success)]">{t("invoices.paid")} {formatDate(invoice.paidAtUtc)}</span>
        ) : (
          "—"
        )}
      </span>
    </EntityListRow>
  );
}
