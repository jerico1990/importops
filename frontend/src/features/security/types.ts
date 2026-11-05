export type SecurityRole = {
  roleId: number;
  code: string;
  name: string;
  isSystem: boolean;
  isActive: boolean;
};

export type SecurityUser = {
  userId: number;
  companyId: number;
  username: string;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  userType: "INTERNAL" | "CLIENT";
  clientId?: number | null;
  clientName?: string | null;
  mustChangePassword: boolean;
  isActive: boolean;
  roles: SecurityRole[];
};

export type CreateSecurityUserRequest = {
  username: string;
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  userType: "INTERNAL" | "CLIENT";
  clientId?: number | null;
  password: string;
  mustChangePassword: boolean;
  roleIds: number[];
};

export type UpdateSecurityUserRequest = {
  email?: string | null;
  firstName?: string | null;
  lastName?: string | null;
  userType: "INTERNAL" | "CLIENT";
  clientId?: number | null;
  roleIds: number[];
};
