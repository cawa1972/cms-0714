/** Response model for the AppRole (角色) table. */
export interface AppRole {
  pkid: number;
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description: string | null;
  /** Count of users assigned to this role (使用者數). */
  userCount: number;
  /** Assigned user ids (populated on GET by id). */
  userIds: string[];
}

/** Write DTO for create/update. roleId is required on create, immutable on edit. */
export interface AppRoleRequest {
  roleId: string;
  roleName: string;
  permissionLevel: number;
  description: string | null;
  userIds: string[];
}

/** Search DTO for POST /api/app-roles/query. */
export interface AppRoleQuery {
  keyword?: string | null;
  permissionLevel?: number | null;
}

/** Slim lookup option (e.g. AppUser: value = userId, label = "UserName (UserId)"). */
export interface LookupItem {
  value: string;
  label: string;
}
