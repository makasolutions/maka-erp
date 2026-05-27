import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "@/auth/use-auth";

export function ProtectedRoute() {
  const { isAuthenticated } = useAuth();
  const location = useLocation();

  if (!isAuthenticated) {
    // If the user just logged out intentionally (flag set by auth-context
    // logout()), do NOT carry the current URL as the post-login destination.
    // Doing so would redirect the NEXT user straight to the previous user's
    // last-visited page (e.g. /identity/roles/<id> → 403/not-found).
    const wasIntentionalLogout =
      sessionStorage.getItem("_app_intentional_logout") === "1";
    if (wasIntentionalLogout) {
      sessionStorage.removeItem("_app_intentional_logout");
      return <Navigate to="/login" replace />;
    }

    // Genuine session expiry: preserve `from` so the user lands back on
    // the page they were on after re-authenticating.
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
