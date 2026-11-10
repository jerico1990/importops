import { api } from "./http";
import type { SessionUser } from "@/types/auth";

export type LoginRequest = {
  username: string;
  password: string;
  rememberMe: boolean;
};

export type LoginResponse = {
  user: SessionUser;
  expiresAt: string;
};

export const authApi = {
  login: (request: LoginRequest) =>
    api.post<LoginResponse>("/api/auth/login", request),

  session: () =>
    api.get<SessionUser>("/api/auth/session"),

  updateProfile: (request: { firstName: string; lastName: string; email: string }) =>
    api.put<SessionUser>("/api/auth/profile", request),

  changePassword: (request: { currentPassword: string; newPassword: string }) =>
    api.post<void>("/api/auth/change-password", request),

  logout: () =>
    api.post<void>("/api/auth/logout")
};
