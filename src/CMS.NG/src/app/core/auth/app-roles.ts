/**
 * Well-known RoleId values, mirroring the backend `CMS.API/Security/AppRoles.cs`. The JWT carries one
 * `role` claim per assigned RoleId, so these are the strings {@link AuthService.hasRole} matches on.
 */
export const AppRoles = {
  Admin: 'Admin',
} as const;

/** Landing route for a user who may enter the Admin area (系統管理). */
const ADMIN_HOME = '/app-roles';

/** Landing route for everyone else — 上稿作業, the first item in the non-admin menu. */
const DEFAULT_HOME = '/featured-promo-items';

/**
 * The route a signed-in user should land on. Admins keep `/app-roles`; everyone else gets
 * `/featured-promo-items`, because the Admin pages are guarded and would bounce them straight back.
 * Used by both the `''` redirect in the route table and {@link adminGuard}'s rejection target, so the
 * two can never disagree about where "home" is.
 */
export function homeRouteFor(roles: readonly string[]): string {
  return roles.includes(AppRoles.Admin) ? ADMIN_HOME : DEFAULT_HOME;
}
