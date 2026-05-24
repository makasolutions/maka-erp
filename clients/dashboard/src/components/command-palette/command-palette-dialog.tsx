import { useMemo } from "react";
import { useNavigate } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { Command } from "cmdk";
import {
  Activity,
  Boxes,
  Folder,
  HeartPulse,
  KeyRound,
  LayoutDashboard,
  LifeBuoy,
  LogOut,
  MessageSquare,
  Monitor,
  Moon,
  Package,
  Palette,
  Plus,
  Receipt,
  ScrollText,
  Search,
  Settings as SettingsIcon,
  Shield,
  ShieldCheck,
  Sparkles,
  Sun,
  Tag,
  Users,
  UserRound,
} from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogTitle,
} from "@/components/ui/dialog";
import { useAuth } from "@/auth/use-auth";
import { useTheme } from "@/components/theme/theme-provider";
import { accents } from "@/components/theme/appearance-options";
import { cn } from "@/lib/cn";

/**
 * Command palette dialog — separated from the provider so cmdk + the full
 * action graph (lucide icons, accent options, navigate logic) are
 * code-split into their own chunk. The provider in command-palette.tsx
 * lazy-imports this module on first ⌘K, keeping the main shell shipping
 * a smaller bundle for cold start.
 */

type ActionItem = {
  id: string;
  label: string;
  hint?: string;
  Icon: React.ComponentType<{ className?: string }>;
  /** Free-form keywords for fuzzy matching. */
  keywords?: string[];
  shortcut?: string;
  perform: () => void;
};

type ActionGroup = {
  heading: string;
  items: ActionItem[];
};

export function CommandPaletteDialog({
  open,
  onOpenChange,
}: {
  open: boolean;
  onOpenChange: (next: boolean) => void;
}) {
  const navigate = useNavigate();
  const { logout } = useAuth();
  const { setMode, setAccent } = useTheme();
  const { t } = useTranslation("common");

  // Build the action set fresh each time the palette opens. The ones
  // that navigate close the palette; the ones that mutate appearance
  // don't, so the user can preview multiple choices.
  const groups = useMemo<ActionGroup[]>(() => {
    const close = () => onOpenChange(false);
    const go = (path: string) => () => {
      navigate(path);
      close();
    };
    return [
      {
        heading: t("commandPalette.groups.navigate"),
        items: [
          {
            id: "nav-overview",
            label: t("commandPalette.items.overview"),
            hint: t("commandPalette.items.overviewHint"),
            Icon: LayoutDashboard,
            keywords: ["home", "dashboard"],
            perform: go("/"),
          },
          {
            id: "nav-activity",
            label: t("commandPalette.items.liveActivity"),
            hint: t("commandPalette.items.liveActivityHint"),
            Icon: Activity,
            keywords: ["events", "sse", "log"],
            perform: go("/activity"),
          },
          {
            id: "nav-chat",
            label: t("commandPalette.items.chat"),
            hint: t("commandPalette.items.chatHint"),
            Icon: MessageSquare,
            keywords: ["messages", "dm", "channel", "conversation"],
            perform: go("/chat"),
          },
          {
            id: "nav-files",
            label: t("commandPalette.items.files"),
            hint: t("commandPalette.items.filesHint"),
            Icon: Folder,
            keywords: ["storage", "uploads", "documents"],
            perform: go("/files"),
          },
          {
            id: "nav-users",
            label: t("commandPalette.items.users"),
            hint: t("commandPalette.items.usersHint"),
            Icon: Users,
            keywords: ["identity", "people", "members", "team"],
            perform: go("/identity/users"),
          },
          {
            id: "nav-roles",
            label: t("commandPalette.items.roles"),
            hint: t("commandPalette.items.rolesHint"),
            Icon: ShieldCheck,
            keywords: ["identity", "permissions", "rbac"],
            perform: go("/identity/roles"),
          },
          {
            id: "nav-groups",
            label: t("commandPalette.items.groups"),
            hint: t("commandPalette.items.groupsHint"),
            Icon: Users,
            keywords: ["identity", "teams", "org"],
            perform: go("/identity/groups"),
          },
          {
            id: "nav-products",
            label: t("commandPalette.items.products"),
            hint: t("commandPalette.items.productsHint"),
            Icon: Package,
            keywords: ["catalog", "sku", "inventory", "stock"],
            perform: go("/catalog/products"),
          },
          {
            id: "nav-brands",
            label: t("commandPalette.items.brands"),
            hint: t("commandPalette.items.brandsHint"),
            Icon: Tag,
            keywords: ["catalog"],
            perform: go("/catalog/brands"),
          },
          {
            id: "nav-categories",
            label: t("commandPalette.items.categories"),
            hint: t("commandPalette.items.categoriesHint"),
            Icon: Boxes,
            keywords: ["catalog"],
            perform: go("/catalog/categories"),
          },
          {
            id: "nav-tickets",
            label: t("commandPalette.items.tickets"),
            hint: t("commandPalette.items.ticketsHint"),
            Icon: LifeBuoy,
            keywords: ["support", "issues", "helpdesk"],
            perform: go("/tickets"),
          },
          {
            id: "nav-invoices",
            label: t("commandPalette.items.invoices"),
            hint: t("commandPalette.items.invoicesHint"),
            Icon: Receipt,
            keywords: ["billing", "payment"],
            perform: go("/invoices"),
          },
          {
            id: "nav-health",
            label: t("commandPalette.items.health"),
            hint: t("commandPalette.items.healthHint"),
            Icon: HeartPulse,
            keywords: ["status", "uptime", "system", "ready", "redis", "postgres"],
            perform: go("/system/health"),
          },
          {
            id: "nav-audits",
            label: t("commandPalette.items.auditTrail"),
            hint: t("commandPalette.items.auditTrailHint"),
            Icon: ScrollText,
            keywords: ["audit", "log", "compliance", "security", "trace", "correlation"],
            perform: go("/system/audits"),
          },
          {
            id: "nav-trash",
            label: t("commandPalette.items.trash"),
            hint: t("commandPalette.items.trashHint"),
            Icon: ScrollText,
            keywords: ["recycle", "deleted", "restore"],
            perform: go("/system/trash"),
          },
          {
            id: "nav-sessions",
            label: t("commandPalette.items.sessions"),
            hint: t("commandPalette.items.sessionsHint"),
            Icon: Shield,
            keywords: ["devices", "logins"],
            perform: go("/system/sessions"),
          },
          {
            id: "nav-settings",
            label: t("commandPalette.items.settings"),
            Icon: SettingsIcon,
            keywords: ["preferences", "config"],
            perform: go("/settings"),
          },
        ],
      },
      {
        heading: t("commandPalette.groups.create"),
        items: [
          {
            id: "create-user",
            label: t("commandPalette.items.createUser"),
            hint: t("commandPalette.items.createUserHint"),
            Icon: Plus,
            keywords: ["new", "invite", "register", "identity"],
            perform: go("/identity/users?action=create"),
          },
          {
            id: "create-role",
            label: t("commandPalette.items.createRole"),
            hint: t("commandPalette.items.createRoleHint"),
            Icon: Plus,
            keywords: ["new", "permissions", "rbac"],
            perform: go("/identity/roles?action=create"),
          },
          {
            id: "create-group",
            label: t("commandPalette.items.createGroup"),
            hint: t("commandPalette.items.createGroupHint"),
            Icon: Plus,
            keywords: ["new", "team", "org"],
            perform: go("/identity/groups?action=create"),
          },
          {
            id: "create-product",
            label: t("commandPalette.items.createProduct"),
            hint: t("commandPalette.items.createProductHint"),
            Icon: Plus,
            keywords: ["new", "catalog", "sku"],
            perform: go("/catalog/products?action=create"),
          },
          {
            id: "create-brand",
            label: t("commandPalette.items.createBrand"),
            hint: t("commandPalette.items.createBrandHint"),
            Icon: Plus,
            keywords: ["new", "catalog"],
            perform: go("/catalog/brands?action=create"),
          },
          {
            id: "create-category",
            label: t("commandPalette.items.createCategory"),
            hint: t("commandPalette.items.createCategoryHint"),
            Icon: Plus,
            keywords: ["new", "catalog"],
            perform: go("/catalog/categories?action=create"),
          },
          {
            id: "create-ticket",
            label: t("commandPalette.items.createTicket"),
            hint: t("commandPalette.items.createTicketHint"),
            Icon: Plus,
            keywords: ["new", "support", "issue"],
            perform: go("/tickets?action=create"),
          },
          {
            id: "create-channel",
            label: t("commandPalette.items.createChannel"),
            hint: t("commandPalette.items.createChannelHint"),
            Icon: Plus,
            keywords: ["new", "chat", "channel"],
            perform: go("/chat?action=create-channel"),
          },
          {
            id: "create-file",
            label: t("commandPalette.items.uploadFile"),
            hint: t("commandPalette.items.uploadFileHint"),
            Icon: Plus,
            keywords: ["new", "upload", "attach"],
            perform: go("/files?action=upload"),
          },
        ],
      },
      {
        heading: t("commandPalette.groups.account"),
        items: [
          {
            id: "acc-profile",
            label: t("commandPalette.items.profile"),
            hint: t("commandPalette.items.profileHint"),
            Icon: UserRound,
            perform: go("/settings/profile"),
          },
          {
            id: "acc-security",
            label: t("commandPalette.items.security"),
            hint: t("commandPalette.items.securityHint"),
            Icon: Shield,
            keywords: ["password", "2fa", "sessions"],
            perform: go("/settings/security"),
          },
          {
            id: "acc-keys",
            label: t("commandPalette.items.apiKeys"),
            hint: t("commandPalette.items.apiKeysHint"),
            Icon: KeyRound,
            keywords: ["token", "credentials"],
            perform: go("/settings/api-keys"),
          },
          {
            id: "acc-notifications",
            label: t("commandPalette.items.notifications"),
            hint: t("commandPalette.items.notificationsHint"),
            Icon: Sparkles,
            perform: go("/settings/notifications"),
          },
          {
            id: "acc-appearance",
            label: t("commandPalette.items.appearance"),
            hint: t("commandPalette.items.appearanceHint"),
            Icon: Palette,
            keywords: ["theme", "font", "density", "dark", "light"],
            perform: go("/settings/appearance"),
          },
        ],
      },
      {
        heading: t("commandPalette.groups.theme"),
        items: [
          {
            id: "theme-light",
            label: t("commandPalette.items.switchLight"),
            Icon: Sun,
            keywords: ["bright", "day"],
            perform: () => setMode("light"),
          },
          {
            id: "theme-dark",
            label: t("commandPalette.items.switchDark"),
            Icon: Moon,
            keywords: ["night", "oled"],
            perform: () => setMode("dark"),
          },
          {
            id: "theme-system",
            label: t("commandPalette.items.followSystem"),
            Icon: Monitor,
            keywords: ["auto"],
            perform: () => setMode("system"),
          },
        ],
      },
      {
        heading: t("commandPalette.groups.accent"),
        items: accents.map((a) => ({
          id: `accent-${a.id}`,
          label: t("commandPalette.items.setAccent", { label: a.label }),
          hint: a.description,
          Icon: Palette,
          keywords: ["color", "brand", a.id],
          perform: () => setAccent(a.id),
        })),
      },
      {
        heading: t("commandPalette.groups.session"),
        items: [
          {
            id: "sess-logout",
            label: t("commandPalette.items.signOut"),
            hint: t("commandPalette.items.signOutHint"),
            Icon: LogOut,
            keywords: ["logout", "exit", "quit"],
            perform: () => {
              close();
              logout();
            },
          },
        ],
      },
    ];
  }, [navigate, onOpenChange, setMode, setAccent, logout, t]);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent
        className={cn(
          "max-w-[640px] p-0 sm:max-w-[640px]",
          "bg-[var(--color-popover)]",
        )}
      >
        <DialogTitle className="sr-only">{t("commandPalette.title")}</DialogTitle>
        <DialogDescription className="sr-only">
          {t("commandPalette.description")}
        </DialogDescription>

        <Command
          loop
          className="flex flex-col"
          // cmdk sets [cmdk-...] data attrs we hook into with selectors below.
        >
          {/* Search row — mirrors EntitySearch shape (rounded-xl, soft icon left). */}
          <div className="flex items-center gap-2.5 border-b border-border px-4 py-3">
            <Search className="h-[18px] w-[18px] shrink-0 text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]" aria-hidden />
            <Command.Input
              placeholder={t("commandPalette.placeholder")}
              aria-label={t("commandPalette.title")}
              className={cn(
                "h-7 flex-1 bg-transparent text-[14px] tracking-tight placeholder:text-[oklch(from_var(--color-muted-foreground)_l_c_h_/_0.5)]",
                "focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
              )}
              autoFocus
            />
            <kbd className="rounded border border-border bg-[var(--color-muted)] px-1.5 py-px text-[10px] tracking-tight text-[var(--color-muted-foreground)]">
              Esc
            </kbd>
          </div>

          {/* Results */}
          <Command.List className="max-h-[420px] overflow-y-auto px-2 py-2">
            <Command.Empty className="px-4 py-12 text-center">
              <p className="text-sm font-medium tracking-tight">{t("commandPalette.noMatches")}</p>
              <p className="mt-1 text-xs text-[var(--color-muted-foreground)]">
                {t("commandPalette.noMatchesDesc")}
              </p>
            </Command.Empty>

            {groups.map((group) => (
              <Command.Group
                key={group.heading}
                heading={group.heading}
                className={cn(
                  // Heading text styling via cmdk's nested rendering.
                  "[&_[cmdk-group-heading]]:px-2 [&_[cmdk-group-heading]]:pb-1 [&_[cmdk-group-heading]]:pt-3",
                  "[&_[cmdk-group-heading]]:text-[11px] [&_[cmdk-group-heading]]:font-semibold",
                  "[&_[cmdk-group-heading]]:uppercase [&_[cmdk-group-heading]]:tracking-wider",
                  "[&_[cmdk-group-heading]]:text-[var(--color-muted-foreground)]",
                )}
              >
                {group.items.map((item) => (
                  <CommandRow key={item.id} item={item} />
                ))}
              </Command.Group>
            ))}
          </Command.List>

          {/* Footer */}
          <div className="flex items-center justify-between border-t border-border px-4 py-2.5">
            <div className="flex items-center gap-3 text-[11px] text-[var(--color-muted-foreground)]">
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↑</kbd>
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↓</kbd>
                {t("commandPalette.navigate")}
              </span>
              <span className="flex items-center gap-1">
                <kbd className="rounded border border-border bg-[var(--color-muted)] px-1 py-px text-[9px]">↵</kbd>
                {t("commandPalette.select")}
              </span>
            </div>
            <span className="text-[11px] text-[var(--color-muted-foreground)]">
              v0.1
            </span>
          </div>
        </Command>
      </DialogContent>
    </Dialog>
  );
}

function CommandRow({ item }: { item: ActionItem }) {
  const { Icon, label, hint, keywords, perform } = item;
  return (
    <Command.Item
      value={[label, hint, ...(keywords ?? [])].filter(Boolean).join(" ")}
      onSelect={perform}
      className={cn(
        "group/cmd flex cursor-default select-none items-center gap-3 rounded-md px-2.5 py-2 text-sm",
        "transition-colors duration-[var(--duration-fast)] ease-[var(--ease-out-cubic)]",
        "outline-none focus:outline-none focus-visible:outline-none focus-visible:shadow-none",
        "hover:bg-[oklch(from_var(--color-accent)_l_c_h_/_0.4)]",
        "data-[selected=true]:bg-[var(--color-primary-soft)] data-[selected=true]:text-[var(--color-foreground)]",
      )}
    >
      <span
        aria-hidden
        className={cn(
          "grid h-7 w-7 shrink-0 place-items-center rounded-md",
          "bg-[var(--color-muted)] text-[var(--color-muted-foreground)]",
          "transition-colors group-data-[selected=true]/cmd:bg-[var(--color-primary-soft)] group-data-[selected=true]/cmd:text-[var(--color-primary)]",
        )}
      >
        <Icon className="h-3.5 w-3.5" />
      </span>
      <span className="flex min-w-0 flex-1 flex-col">
        <span className="truncate font-medium tracking-tight">{label}</span>
        {hint && (
          <span className="truncate text-[11px] text-[var(--color-muted-foreground)]">
            {hint}
          </span>
        )}
      </span>
    </Command.Item>
  );
}
