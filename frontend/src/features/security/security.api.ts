import { api } from "@/lib/api/http";
import type { PagedResult } from "@/types/common";
import type { CreateSecurityUserRequest, SecurityRole, SecurityUser, UpdateSecurityUserRequest } from "./types";

function queryString(values: Record<string, string | number | boolean | undefined | null>) {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && String(value).trim() !== "") params.set(key, String(value));
  });
  const text = params.toString();
  return text ? `?${text}` : "";
}

export const securityApi = {
  users: (query: { search?: string; userType?: string; clientId?: number; isActive?: boolean; page: number; pageSize: number }) =>
    api.get<PagedResult<SecurityUser>>(`/api/security/users${queryString(query)}`),
  roles: () => api.get<SecurityRole[]>("/api/security/roles"),
  createUser: (request: CreateSecurityUserRequest) => api.post<SecurityUser>("/api/security/users", request),
  updateUser: (userId: number, request: UpdateSecurityUserRequest) => api.put<SecurityUser>(`/api/security/users/${userId}`, request),
  setActive: (userId: number, isActive: boolean) => api.put<SecurityUser>(`/api/security/users/${userId}/active`, { isActive }),
  resetPassword: (userId: number, request: { newPassword: string; mustChangePassword: boolean }) =>
    api.post<void>(`/api/security/users/${userId}/reset-password`, request)
};
