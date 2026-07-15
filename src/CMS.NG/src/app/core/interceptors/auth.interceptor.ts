import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '@core/services/auth.service';

/** Shown when a 5xx response carries no usable body (e.g. the API is down entirely). */
const FALLBACK_SERVER_ERROR_MESSAGE = '發生未預期的錯誤。(An unexpected error occurred.)';

/**
 * Attaches `Authorization: Bearer <token>` (when a token is in session storage) to every outgoing
 * request. On any 401 response, clears the session and redirects to the login page; on any 5xx
 * response, shows a friendly error toast carrying only the backend's safe generic message. Other
 * statuses (validation 400s, 403, …) pass through so callers keep handling them per form.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const messages = inject(MessageService);

  const token = auth.token;
  const authorized = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authorized).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401) {
          auth.clearSession();
          router.navigate(['/login']);
        } else if (error.status >= 500) {
          // The exception middleware guarantees the body is { message: <safe generic text> }.
          const body = error.error as { message?: unknown } | null;
          const detail =
            typeof body?.message === 'string' && body.message.length > 0
              ? body.message
              : FALLBACK_SERVER_ERROR_MESSAGE;
          messages.add({ severity: 'error', summary: '系統錯誤', detail });
        }
      }
      return throwError(() => error);
    }),
  );
};
