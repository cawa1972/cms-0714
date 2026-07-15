/** Response model for the AppUser (使用者) table. PasswordHash is backend-only and never present. */
export interface AppUser {
  pkid: number;
  userId: string;
  userName: string;
  isActive: boolean;
  /** ISO datetime, or null. Dapper returns Kind=Unspecified — append 'Z' before display. */
  passwordUpdatedTime: string | null;
  /** Count of roles assigned to this user (角色數). */
  roleCount: number;
  /** Assigned role ids (populated on GET by id). */
  roleIds: string[];
}

/**
 * Write DTO for create/update. userId is required on create, immutable on edit.
 * There is deliberately no password field — the API sets it server-side.
 */
export interface AppUserRequest {
  userId: string;
  userName: string;
  isActive: boolean;
  roleIds: string[];
}

/** Search DTO for POST /api/app-users/query. */
export interface AppUserQuery {
  keyword?: string | null;
  isActive?: boolean | null;
}
