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
