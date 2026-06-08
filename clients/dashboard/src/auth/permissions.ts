/**
 * P — Single source of truth for permission strings.
 *
 * Mirrors the backend `Permissions.{Resource}.{Action}` shape defined in
 * the .Contracts/Authorization/*.cs files. Use this object everywhere in
 * the frontend instead of inline string literals so that:
 *   1. TypeScript autocomplete catches typos at edit-time.
 *   2. A backend rename only requires updating one place here.
 *   3. Three formerly-duplicate local objects (routes.tsx, nav-data.ts,
 *      command-palette-dialog.tsx) are replaced by a single import.
 *
 * Usage:
 *   import { P } from "@/auth/permissions";
 *   <Button perm={P.catalog.brands.create}>Create Brand</Button>
 *   <Perm need={P.catalog.brands.update}>…</Perm>
 *   const { can } = usePerm();
 *   if (can(P.identity.roles.delete)) { … }
 */

export const P = {
  // ── Lookups (Tablas Básicas) ───────────────────────────────────────────────
  lookups: {
    tables: {
      view:   "Permissions.Lookups.Tables.View",
      create: "Permissions.Lookups.Tables.Create",
      update: "Permissions.Lookups.Tables.Update",
      delete: "Permissions.Lookups.Tables.Delete",
    },
    global: { manage: "Permissions.Lookups.Global.Manage" },
  },
  // ── Parties (Terceros) ──────────────────────────────────────────────────────
  parties: {
    view:    "Permissions.Parties.Parties.View",
    create:  "Permissions.Parties.Parties.Create",
    update:  "Permissions.Parties.Parties.Update",
    delete:  "Permissions.Parties.Parties.Delete",
    restore: "Permissions.Parties.Parties.Restore",
  },
  // ── HR (Empleados) ──────────────────────────────────────────────────────────
  hr: {
    employees: {
      view:   "Permissions.Hr.Employees.View",
      create: "Permissions.Hr.Employees.Create",
      update: "Permissions.Hr.Employees.Update",
      delete: "Permissions.Hr.Employees.Delete",
    },
  },
  // ── Catalog ──────────────────────────────────────────────────────────────
  catalog: {
    brands: {
      view:    "Permissions.Catalog.Brands.View",
      create:  "Permissions.Catalog.Brands.Create",
      update:  "Permissions.Catalog.Brands.Update",
      delete:  "Permissions.Catalog.Brands.Delete",
      restore: "Permissions.Catalog.Brands.Restore",
    },
    categories: {
      view:    "Permissions.Catalog.Categories.View",
      create:  "Permissions.Catalog.Categories.Create",
      update:  "Permissions.Catalog.Categories.Update",
      delete:  "Permissions.Catalog.Categories.Delete",
      restore: "Permissions.Catalog.Categories.Restore",
    },
    products: {
      view:         "Permissions.Catalog.Products.View",
      create:       "Permissions.Catalog.Products.Create",
      update:       "Permissions.Catalog.Products.Update",
      delete:       "Permissions.Catalog.Products.Delete",
      restore:      "Permissions.Catalog.Products.Restore",
      publish:      "Permissions.Catalog.Products.Publish",
      archive:      "Permissions.Catalog.Products.Archive",
      adjustStock:  "Permissions.Catalog.Products.AdjustStock",
    },
    attributes: {
      view:   "Permissions.Catalog.Attributes.View",
      manage: "Permissions.Catalog.Attributes.Manage",
    },
    priceLists: {
      view:        "Permissions.Catalog.PriceLists.View",
      manage:      "Permissions.Catalog.PriceLists.Manage",
      approveBulk: "Permissions.Catalog.PriceLists.ApproveBulk",
    },
  },

  // ── Identity ─────────────────────────────────────────────────────────────
  identity: {
    users: {
      view:        "Permissions.Users.View",
      search:      "Permissions.Users.Search",
      create:      "Permissions.Users.Create",
      update:      "Permissions.Users.Update",
      delete:      "Permissions.Users.Delete",
      export:      "Permissions.Users.Export",
      manageRoles: "Permissions.Users.ManageRoles",
      impersonate: "Permissions.Users.Impersonate",
    },
    userRoles: {
      view:   "Permissions.UserRoles.View",
      update: "Permissions.UserRoles.Update",
    },
    roles: {
      view:   "Permissions.Roles.View",
      create: "Permissions.Roles.Create",
      update: "Permissions.Roles.Update",
      delete: "Permissions.Roles.Delete",
    },
    roleClaims: {
      view:   "Permissions.RoleClaims.View",
      update: "Permissions.RoleClaims.Update",
    },
    sessions: {
      view:      "Permissions.Sessions.View",
      revoke:    "Permissions.Sessions.Revoke",
      viewAll:   "Permissions.Sessions.ViewAll",
      revokeAll: "Permissions.Sessions.RevokeAll",
    },
    groups: {
      view:          "Permissions.Groups.View",
      create:        "Permissions.Groups.Create",
      update:        "Permissions.Groups.Update",
      delete:        "Permissions.Groups.Delete",
      manageMembers: "Permissions.Groups.ManageMembers",
    },
    impersonation: {
      view:   "Permissions.Impersonation.View",
      revoke: "Permissions.Impersonation.Revoke",
    },
    localization: {
      view:   "Permissions.Localization.View",
      update: "Permissions.Localization.Update",
    },
    appearance: {
      view:   "Permissions.Appearance.View",
      update: "Permissions.Appearance.Update",
    },
  },

  // ── Billing ──────────────────────────────────────────────────────────────
  billing: {
    view:   "Permissions.Billing.View",
    manage: "Permissions.Billing.Manage",
  },

  // ── Tickets ──────────────────────────────────────────────────────────────
  tickets: {
    view:    "Permissions.Tickets.View",
    create:  "Permissions.Tickets.Create",
    update:  "Permissions.Tickets.Update",
    delete:  "Permissions.Tickets.Delete",
    restore: "Permissions.Tickets.Restore",
    assign:  "Permissions.Tickets.Assign",
    resolve: "Permissions.Tickets.Resolve",
    reopen:  "Permissions.Tickets.Reopen",
    comment: "Permissions.Tickets.Comment",
  },

  // ── Auditing ─────────────────────────────────────────────────────────────
  auditTrails: {
    view:            "Permissions.AuditTrails.View",
    viewCrossTenant: "Permissions.AuditTrails.ViewCrossTenant",
  },

  // ── Files ─────────────────────────────────────────────────────────────────
  files: {
    upload:    "Permissions.Files.Upload",
    deleteOwn: "Permissions.Files.DeleteOwn",
    deleteAny: "Permissions.Files.DeleteAny",
    viewTrash: "Permissions.Files.ViewTrash",
    restore:   "Permissions.Files.Restore",
  },

  // ── Chat ──────────────────────────────────────────────────────────────────
  chat: {
    channels: {
      view:      "Permissions.Chat.Channels.View",
      create:    "Permissions.Chat.Channels.Create",
      manageAll: "Permissions.Chat.Channels.ManageAll",
    },
    messages: {
      send:      "Permissions.Chat.Messages.Send",
      editOwn:   "Permissions.Chat.Messages.EditOwn",
      deleteOwn: "Permissions.Chat.Messages.DeleteOwn",
      deleteAny: "Permissions.Chat.Messages.DeleteAny",
    },
  },

  // ── Notifications ─────────────────────────────────────────────────────────
  notifications: {
    inbox: {
      view:     "Permissions.Notifications.Inbox.View",
      markRead: "Permissions.Notifications.Inbox.MarkRead",
    },
  },

  // ── Multitenancy ─────────────────────────────────────────────────────────
  multitenancy: {
    tenants: {
      view:               "Permissions.Tenants.View",
      create:             "Permissions.Tenants.Create",
      update:             "Permissions.Tenants.Update",
      upgradeSubscription:"Permissions.Tenants.UpgradeSubscription",
      viewTheme:          "Permissions.Tenants.ViewTheme",
      updateTheme:        "Permissions.Tenants.UpdateTheme",
    },
  },
} as const;

/** Inferred union type of all permission strings. */
export type Permission = (typeof P)[keyof typeof P] extends infer V
  ? V extends Record<string, unknown>
    ? never
    : V
  : never;
