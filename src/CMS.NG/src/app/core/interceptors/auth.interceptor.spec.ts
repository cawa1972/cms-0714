import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';

import { authInterceptor } from './auth.interceptor';
import { AuthProfile } from '@core/models/auth.model';

const STORAGE_KEY = 'cms.auth';

function storeProfile(accessToken: string): void {
  const profile: AuthProfile = { userId: 'helen', userName: 'Helen Chen', accessToken };
  sessionStorage.setItem(STORAGE_KEY, JSON.stringify(profile));
}

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
    sessionStorage.clear();
  });

  it('attaches the Bearer token from session storage to outgoing requests', () => {
    storeProfile('the-access-token');

    http.get('/api/app-roles').subscribe();

    const req = httpMock.expectOne('/api/app-roles');
    expect(req.request.headers.get('Authorization')).toBe('Bearer the-access-token');
    req.flush([]);
  });

  it('does not attach an Authorization header when there is no token', () => {
    http.get('/api/app-roles').subscribe();

    const req = httpMock.expectOne('/api/app-roles');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });

  it('on a 401 clears session storage and redirects to /login', () => {
    storeProfile('expired-token');
    const navigate = spyOn(router, 'navigate');

    http.get('/api/app-roles').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/app-roles');
    req.flush('unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(sessionStorage.getItem(STORAGE_KEY)).toBeNull();
    expect(navigate).toHaveBeenCalledWith(['/login']);
  });
});
