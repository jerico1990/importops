import { Navigate, Outlet } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function PermissionRoute({
  anyOf
}: {
  anyOf: string[];
}) {
  const { hasAnyPermission } = useAuth();

  if (!hasAnyPermission(...anyOf)) {
    return <Navigate to="/403" replace />;
  }

  return <Outlet />;
}
