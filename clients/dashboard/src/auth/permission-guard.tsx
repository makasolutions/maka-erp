import { useCallback, type ReactNode } from "react";
import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "@/auth/use-auth";

/**
 * Protects a **group** of routes (use as a parent route element with
 * `<Outlet />`). Redirects to `/` if the user lacks `permission`.
 *
 * Example in routes.tsx:
 * ```
 * {
 *   element: <PermissionRoute permission="Permissions.Users.View" />,
 *   children: [
 *     { path: "identity/users", element: withSuspense(<UsersPage />) },
 *     { path: "identity/users/:userId", element: withSuspense(<UserDetailPage />) },
 *   ],
 * }
 * ```
 */
export function PermissionRoute({ permission }: { permission: string }) {
  const { user } = useAuth();
  if (!user?.permissions.includes(permission)) {
    return <Navigate to="/" replace />;
  }
  return <Outlet />;
}

/**
 * Wraps a **single** element with a permission check.
 *
 * Example:
 * ```
 * { path: "system/audits",
 *   element: withSuspense(
 *     <PermissionGuard permission="Permissions.AuditTrails.View">
 *       <AuditsPage />
 *     </PermissionGuard>
 *   )
 * }
 * ```
 */
export function PermissionGuard({
  permission,
  children,
}: {
  permission: string;
  children: React.ReactNode;
}) {
  const { user } = useAuth();
  if (!user?.permissions.includes(permission)) {
    return <Navigate to="/" replace />;
  }
  return <>{children}</>;
}

/**
 * Returns `true` if the currently logged-in user holds `permission`.
 * Pass `undefined` to skip the check (always returns `true`).
 * Use this hook inside components to conditionally render UI elements.
 */
export function useHasPermission(permission: string | undefined): boolean {
  const { user } = useAuth();
  if (!permission) return true;
  if (!user) return false;
  return user.permissions.includes(permission);
}

// ─── Declarative permission primitives ───────────────────────────────────────

/**
 * `usePerm` — base hook for permission checks.
 *
 * Returns a stable `can(permission)` function so a single call replaces
 * N × `useHasPermission` calls in pages that guard multiple actions.
 *
 * @example
 * ```tsx
 * const { can } = usePerm();
 * const canEdit   = can(P.catalog.brands.update);
 * const canDelete = can(P.catalog.brands.delete);
 * ```
 */
export function usePerm() {
  const { user } = useAuth();
  const can = useCallback(
    (permission: string) => user?.permissions.includes(permission) ?? false,
    [user],
  );
  return { can };
}

/**
 * `<Perm>` — declarative permission gate.
 *
 * Renders `children` only when the user holds the required permission(s).
 * Falls back to `fallback` (default `null`) when access is denied.
 *
 * @param need   - Single permission string, or array of strings.
 * @param mode   - `"any"` (OR — default) or `"all"` (AND).
 * @param fallback - What to render when denied. Defaults to nothing.
 *
 * @example
 * ```tsx
 * <Perm need={P.catalog.brands.create}>
 *   <Button onClick={onCreate}>Create brand</Button>
 * </Perm>
 *
 * <Perm need={[P.catalog.brands.update, P.catalog.brands.delete]} mode="any">
 *   <RowActions brand={brand} />
 * </Perm>
 * ```
 */
export function Perm({
  need,
  mode = "any",
  children,
  fallback = null,
}: {
  need: string | string[];
  mode?: "any" | "all";
  children: ReactNode;
  fallback?: ReactNode;
}) {
  const { can } = usePerm();
  const allowed = Array.isArray(need)
    ? mode === "all"
      ? need.every(can)
      : need.some(can)
    : can(need);
  return allowed ? <>{children}</> : <>{fallback}</>;
}
