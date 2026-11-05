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

  logout: () =>
    api.post<void>("/api/auth/logout")
};
