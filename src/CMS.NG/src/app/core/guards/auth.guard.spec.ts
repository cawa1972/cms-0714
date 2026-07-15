import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router, UrlTree } from '@angular/router';
import { ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';

import { authGuard } from './auth.guard';
import { AuthProfile } from '@core/models/auth.model';

const STORAGE_KEY = 'cms.auth';

function runGuard(): boolean | UrlTree {
  return TestBed.runInInjectionContext(() =>
    authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
  ) as boolean | UrlTree;
}

describe('authGuard', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    });
  });

  afterEach(() => sessionStorage.clear());

  it('redirects to /login when there is no token in session storage', () => {
    const result = runGuard();

    expect(result instanceof UrlTree).toBeTrue();
    expect((result as UrlTree).toString()).toBe('/login');
  });

  it('allows activation when a token is present', () => {
    const profile: AuthProfile = { userId: 'helen', userName: 'Helen Chen', accessToken: 'a-token' };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(profile));

    const result = runGuard();

    expect(result).toBeTrue();
  });
});
