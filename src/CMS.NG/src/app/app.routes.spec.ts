import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { Router, provideRouter } from '@angular/router';
import { MessageService } from 'primeng/api';

import { routes } from './app.routes';
import { AuthProfile } from '@core/models/auth.model';

const STORAGE_KEY = 'cms.auth';

function tokenWithRoles(roles: string[]): string {
  const payload = { userId: 'helen', userName: 'Helen Chen', role: roles };
  const encoded = btoa(JSON.stringify(payload)).replace(/\+/g, '-').replace(/\//g, '_');
  return `header.${encoded}.signature`;
}

function signIn(roles: string[]): void {
  const profile: AuthProfile = {
    userId: 'helen',
    userName: 'Helen Chen',
    accessToken: tokenWithRoles(roles),
  };
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
}

/**
 * Exercises the real route table through the real Router, so the wiring itself is under test: the
 * `''` redirect function, the path-less adminGuard parent, and the wildcard. The guard unit specs
 * cover the decision; these cover that it is actually attached to the right routes.
 */
describe('app.routes (Admin gating)', () => {
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MessageService, useValue: jasmine.createSpyObj('MessageService', ['add']) },
      ],
    });
    router = TestBed.inject(Router);
  });

  afterEach(() => sessionStorage.clear());

  it('sends a signed-out visitor to /login', async () => {
    await router.navigateByUrl('/app-users');

    expect(router.url).toBe('/login');
  });

  describe('as an admin', () => {
    beforeEach(() => signIn(['Admin']));

    it('lands on /app-roles from the root', async () => {
      await router.navigateByUrl('/');

      expect(router.url).toBe('/app-roles');
    });

    it('reaches every Admin area', async () => {
      for (const url of ['/app-users', '/app-roles', '/publish-statuses']) {
        await router.navigateByUrl(url);
        expect(router.url).toBe(url);
      }
    });

    it('reaches a nested Admin route', async () => {
      await router.navigateByUrl('/app-users/helen/edit');

      expect(router.url).toBe('/app-users/helen/edit');
    });
  });

  describe('as a non-admin', () => {
    beforeEach(() => signIn(['Editor']));

    it('lands on /featured-promo-items from the root, not the Admin page', async () => {
      await router.navigateByUrl('/');

      expect(router.url).toBe('/featured-promo-items');
    });

    it('is bounced off every Admin area', async () => {
      for (const url of ['/app-users', '/app-roles', '/publish-statuses']) {
        await router.navigateByUrl(url);
        expect(router.url).toBe('/featured-promo-items');
      }
    });

    it('is bounced off a nested Admin route', async () => {
      await router.navigateByUrl('/app-users/helen/edit');

      expect(router.url).toBe('/featured-promo-items');
    });

    it('still reaches its own non-admin routes', async () => {
      for (const url of ['/courses', '/partners', '/profile']) {
        await router.navigateByUrl(url);
        expect(router.url).toBe(url);
      }
    });

    it('resolves an unknown URL to its own home, not the Admin page', async () => {
      await router.navigateByUrl('/no-such-page');

      expect(router.url).toBe('/featured-promo-items');
    });
  });
});
