import {
  Activity,
  DollarSign,
  FolderOpen,
  FolderTree,
  HeartPulse,
  LayoutDashboard,
  LayoutGrid,
  MessageCircle,
  Package,
  Receipt,
  ScrollText,
  Settings,
  ShieldCheck,
  SlidersHorizontal,
  Tags,
  Ticket,
  Trash2,
  Users,
  UsersRound,
  Wifi,
} from "lucide-react";
import { P } from "@/auth/permissions";

export type NavSpec = {
  to: string;
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  /**
   * When set, this item is only rendered for users who hold this permission.
   * Omit (or leave `undefined`) for items every authenticated user should see.
   * Strings must match the backend's `Permissions.{Resource}.{Action}` shape.
   */
  permission?: string;
};

export type NavSection = {
  id: string;
  caption: string;
  /** Section-level icon used as a fallback when the sidebar is
   *  collapsed and the section is rendered as a stack of item icons. */
  icon: React.ComponentType<{ className?: string }>;
  items: NavSpec[];
};

// Top-level items live OUTSIDE any section. Overview opens the app;
// Settings is account-scoped and lives at the very bottom.
export const topNavTop: NavSpec[] = [
  { to: "/", label: "Overview", icon: LayoutDashboard },
  // Chat requires at least View-Channels permission
  { to: "/chat", label: "Chat", icon: MessageCircle, permission: P.chat.channels.view },
  // Files: Upload + DeleteOwn are IsBasic → every authenticated user has access
  { to: "/files", label: "My Files", icon: FolderOpen },
];

export const topNavBottom: NavSpec[] = [
  // Settings is personal — always visible to any authenticated user
  { to: "/settings", label: "Settings", icon: Settings },
];

// Section accordion. Single-select — only one section open at a time.
// A section is automatically hidden when ALL its items are filtered out.
export const sections: NavSection[] = [
  {
    id: "operations",
    caption: "Operations",
    icon: Activity,
    items: [
      // Live activity: operational stream — no specific permission required
      { to: "/activity", label: "Live activity", icon: Activity },
      { to: "/invoices", label: "Invoices", icon: Receipt, permission: P.billing.view },
    ],
  },
  {
    id: "catalog",
    caption: "Catalog",
    icon: Package,
    items: [
      { to: "/catalog/products",   label: "Products",   icon: Package,    permission: P.catalog.products.view },
      { to: "/catalog/brands",     label: "Brands",     icon: Tags,       permission: P.catalog.brands.view },
      { to: "/catalog/categories", label: "Categories", icon: FolderTree, permission: P.catalog.categories.view },
      { to: "/catalog/attributes", label: "Attributes", icon: SlidersHorizontal, permission: P.catalog.attributes.view },
      { to: "/catalog/price-lists", label: "Price lists", icon: DollarSign, permission: P.catalog.priceLists.view },
    ],
  },
  {
    id: "helpdesk",
    caption: "Helpdesk",
    icon: Ticket,
    items: [
      { to: "/tickets", label: "Tickets", icon: Ticket, permission: P.tickets.view },
    ],
  },
  {
    id: "identity",
    caption: "Identity",
    icon: Users,
    items: [
      { to: "/identity/users",  label: "Users",  icon: Users,       permission: P.identity.users.view },
      { to: "/identity/roles",  label: "Roles",  icon: ShieldCheck, permission: P.identity.roles.view },
      { to: "/identity/groups", label: "Groups", icon: UsersRound,  permission: P.identity.groups.view },
    ],
  },
  {
    id: "system",
    caption: "System",
    icon: HeartPulse,
    items: [
      // Health: operational dashboard — no specific permission required
      { to: "/system/health",   label: "Health",       icon: HeartPulse },
      { to: "/system/audits",   label: "Audit trail",  icon: ScrollText, permission: P.auditTrails.view },
      // Sessions list requires ViewAll (ordinary users only see their own via Settings > Security)
      { to: "/system/sessions", label: "Sessions",     icon: Wifi,       permission: P.identity.sessions.viewAll },
      { to: "/system/trash",    label: "Trash",        icon: Trash2,     permission: P.files.viewTrash },
      // DEV tool — remove before first production release
      { to: "/maka-components", label: "Maka Components", icon: LayoutGrid },
    ],
  },
];

/** Find the section whose items contain the given path (best prefix match). */
export function findSectionForPath(pathname: string): string | null {
  let bestId: string | null = null;
  let bestLen = 0;
  for (const s of sections) {
    for (const item of s.items) {
      if (
        (item.to === "/" && pathname === "/") ||
        (item.to !== "/" && pathname.startsWith(item.to))
      ) {
        if (item.to.length > bestLen) {
          bestLen = item.to.length;
          bestId = s.id;
        }
      }
    }
  }
  return bestId;
}
