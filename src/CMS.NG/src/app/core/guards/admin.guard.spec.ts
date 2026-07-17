import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, UrlTree } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { MessageService } from 'primeng/api';

import { adminGuard } from './admin.guard';
import { AuthProfile } from '@core/models/auth.model';

const STORAGE_KEY = 'cms.auth';

/**
 * Build a JWT whose payload carries the given role claim(s). Only the payload segment is read
 * (AuthService base64url-decodes it); the signature is never verified client-side.
 */
function tokenWithRoles(roles: string[] | string | null): string {
  const payload: Record<string, unknown> = { userId: 'helen', userName: 'Helen Chen' };
  if (roles !== null) {
    payload['role'] = roles;
  }
  const encoded = btoa(JSON.stringify(payload)).replace(/\+/g, '-').replace(/\//g, '_');
  return `header.${encoded}.signature`;
}

function signIn(roles: string[] | string | null): void {
  const profile: AuthProfile = {
    userId: 'helen',
    userName: 'Helen Chen',
    accessToken: tokenWithRoles(roles),
  };
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
}

function runGuard(): boolean | UrlTree {
  return TestBed.runInInjectionContext(() =>
    adminGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  ) as boolean | UrlTree;
}

describe('adminGuard', () => {
  let messages: jasmine.SpyObj<MessageService>;

  beforeEach(() => {
    sessionStorage.clear();
    messages = jasmine.createSpyObj<MessageService>('MessageService', ['add']);
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MessageService, useValue: messages },
      ],
    });
  });

  afterEach(() => sessionStorage.clear());

  it('allows activation when the token carries the Admin role', () => {
    signIn(['Admin']);

    expect(runGuard()).toBeTrue();
    expect(messages.add).not.toHaveBeenCalled();
  });

  it('allows activation when Admin is one of several roles', () => {
    signIn(['Editor', 'Admin']);

    expect(runGuard()).toBeTrue();
  });

  it('redirects a non-admin to the non-admin home route', () => {
    signIn(['Editor']);

    const result = runGuard();

    expect(result instanceof UrlTree).toBeTrue();
    expect((result as UrlTree).toString()).toBe('/featured-promo-items');
  });

  it('shows a permission toast when it rejects a non-admin', () => {
    signIn(['Editor']);

    runGuard();

    expect(messages.add).toHaveBeenCalledTimes(1);
    const toast = messages.add.calls.mostRecent().args[0];
    expect(toast.severity).toBe('warn');
    expect(toast.detail).toContain('沒有權限');
  });

  it('redirects a user whose token carries no role claim at all', () => {
    signIn(null);

    const result = runGuard();

    expect(result instanceof UrlTree).toBeTrue();
    expect((result as UrlTree).toString()).toBe('/featured-promo-items');
  });

  it('does not treat a role merely containing "Admin" as the Admin role', () => {
    signIn(['NotAdminReally']);

    expect(runGuard() instanceof UrlTree).toBeTrue();
  });
});
