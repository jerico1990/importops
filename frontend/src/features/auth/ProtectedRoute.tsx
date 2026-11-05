import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";
import { PageLoader } from "@/components/feedback/PageLoader";

export function ProtectedRoute() {
  const { user, checkingSession } = useAuth();
  const location = useLocation();

  if (checkingSession) {
    return <PageLoader label="Validando sesión..." />;
  }

  if (!user) {
    return (
      <Navigate
        to="/login"
        replace
        state={{ from: location.pathname }}
      />
    );
  }

  return <Outlet />;
}
