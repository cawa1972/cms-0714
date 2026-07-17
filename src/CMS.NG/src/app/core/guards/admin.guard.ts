import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { AuthService } from '@core/services/auth.service';
import { AppRoles, homeRouteFor } from '@core/auth/app-roles';

/** Shown when a non-admin reaches an Admin route directly (typed URL, bookmark, back button). */
const FORBIDDEN_MESSAGE = '沒有權限存取此頁面。(You do not have permission to access this page.)';

/**
 * Blocks the 系統管理 Admin routes for users whose JWT carries no `Admin` role, sending them back to
 * their own home route with an explanatory toast. Stacks after {@link authGuard}, which owns the
 * signed-out case — an unauthenticated user is redirected to /login and never reaches this guard.
 *
 * This mirrors the backend's controller-wide `[Authorize(Roles = AppRoles.Admin)]`; it is a UX
 * affordance, not the security boundary. The API rejects an unauthorized caller with 403 regardless
 * of what the client routes to.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const messages = inject(MessageService);

  if (auth.hasRole(AppRoles.Admin)) {
    return true;
  }

  messages.add({ severity: 'warn', summary: '無權限', detail: FORBIDDEN_MESSAGE });
  return router.createUrlTree([homeRouteFor(auth.roles())]);
};
