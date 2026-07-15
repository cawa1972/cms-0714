import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthService } from './auth.service';
import { AuthProfile } from '@core/models/auth.model';
import { environment } from '@env/environment';

const STORAGE_KEY = 'cms.auth';

/** Build a (signature-less) JWT whose payload carries the given claims. */
function makeJwt(payload: Record<string, unknown>): string {
  const b64 = btoa(JSON.stringify(payload))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
  return `header.${b64}.signature`;
}

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const loginUrl = `${environment.apiBaseUrl}/Auth/login`;

  beforeEach(() => {
    sessionStorage.clear();
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
    localStorage.clear();
  });

  it('login POSTs credentials and stores the profile in SESSION storage (not local)', () => {
    const profile: AuthProfile = {
      userId: 'helen',
      userName: 'Helen Chen',
      accessToken: makeJwt({ userId: 'helen', role: ['Admin'] }),
    };

    service.login({ userId: 'helen', password: 'pw' }).subscribe();

    const req = httpMock.expectOne(loginUrl);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ userId: 'helen', password: 'pw' });
    req.flush(profile);

    expect(sessionStorage.getItem(STORAGE_KEY)).toBeTruthy();
    expect(localStorage.getItem(STORAGE_KEY)).toBeNull();
    expect(service.isAuthenticated()).toBeTrue();
    expect(service.userName()).toBe('Helen Chen');
    expect(service.token).toBe(profile.accessToken);
  });

  it('decodes the roles carried in the JWT (read from the token, not an API call)', () => {
    const profile: AuthProfile = {
      userId: 'helen',
      userName: 'Helen Chen',
      accessToken: makeJwt({ role: ['Admin', 'Editor'] }),
    };
    service.login({ userId: 'helen', password: 'pw' }).subscribe();
    httpMock.expectOne(loginUrl).flush(profile);

    expect(service.roles()).toEqual(['Admin', 'Editor']);
    expect(service.hasRole('Admin')).toBeTrue();
    expect(service.hasRole('Viewer')).toBeFalse();
  });

  it('clearSession removes the profile from session storage', () => {
    const profile: AuthProfile = {
      userId: 'helen',
      userName: 'Helen Chen',
      accessToken: makeJwt({ role: 'Admin' }),
    };
    service.login({ userId: 'helen', password: 'pw' }).subscribe();
    httpMock.expectOne(loginUrl).flush(profile);

    service.clearSession();

    expect(sessionStorage.getItem(STORAGE_KEY)).toBeNull();
    expect(service.isAuthenticated()).toBeFalse();
    expect(service.roles()).toEqual([]);
  });

  it('normalizes a single-string role claim to an array', () => {
    const profile: AuthProfile = {
      userId: 'helen',
      userName: 'Helen Chen',
      accessToken: makeJwt({ role: 'Admin' }),
    };
    service.login({ userId: 'helen', password: 'pw' }).subscribe();
    httpMock.expectOne(loginUrl).flush(profile);

    expect(service.roles()).toEqual(['Admin']);
  });
});
