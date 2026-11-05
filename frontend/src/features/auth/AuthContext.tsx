import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode
} from "react";
import { authApi, type LoginRequest } from "@/lib/api/auth.api";
import { HttpError } from "@/lib/api/http";
import type { SessionUser } from "@/types/auth";

type AuthContextValue = {
  user: SessionUser | null;
  checkingSession: boolean;
  loginPending: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => Promise<void>;
  refreshSession: () => Promise<void>;
  hasPermission: (permission: string) => boolean;
  hasAnyPermission: (...permissions: string[]) => boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [checkingSession, setCheckingSession] = useState(true);
  const [loginPending, setLoginPending] = useState(false);

  const refreshSession = useCallback(async () => {
    try {
      const current = await authApi.session();
      setUser(current);
    } catch (error) {
      if (error instanceof HttpError && error.status === 401) {
        setUser(null);
        return;
      }

      throw error;
    }
  }, []);

  useEffect(() => {
    refreshSession()
      .catch(() => setUser(null))
      .finally(() => setCheckingSession(false));
  }, [refreshSession]);

  const login = useCallback(async (request: LoginRequest) => {
    setLoginPending(true);
    try {
      const result = await authApi.login(request);
      setUser(result.user);
    } finally {
      setLoginPending(false);
    }
  }, []);

  const logout = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      setUser(null);
    }
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      checkingSession,
      loginPending,
      login,
      logout,
      refreshSession,
      hasPermission: (permission) =>
        Boolean(user?.permissions.includes(permission)),
      hasAnyPermission: (...permissions) =>
        permissions.some((permission) =>
          user?.permissions.includes(permission)
        )
    }),
    [user, checkingSession, loginPending, login, logout, refreshSession]
  );

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth debe utilizarse dentro de AuthProvider.");
  }
  return context;
}
