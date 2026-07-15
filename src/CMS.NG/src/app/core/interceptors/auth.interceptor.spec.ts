import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { MessageService } from 'primeng/api';

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
  let messages: MessageService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
        MessageService,
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    messages = TestBed.inject(MessageService);
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

  it('on a 500 shows a friendly toast with the safe message from the response body', () => {
    storeProfile('valid-token');
    const navigate = spyOn(router, 'navigate');
    const add = spyOn(messages, 'add');

    http.get('/api/app-roles').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/app-roles');
    req.flush(
      { message: 'An unexpected error occurred.' },
      { status: 500, statusText: 'Internal Server Error' },
    );

    expect(add).toHaveBeenCalledWith(
      jasmine.objectContaining({ severity: 'error', detail: 'An unexpected error occurred.' }),
    );
    // 5xx must not disturb the session or navigate anywhere.
    expect(sessionStorage.getItem(STORAGE_KEY)).not.toBeNull();
    expect(navigate).not.toHaveBeenCalled();
  });

  it('on a 5xx without a usable body falls back to the generic bilingual message', () => {
    const add = spyOn(messages, 'add');

    http.get('/api/app-roles').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/app-roles');
    req.flush('Bad Gateway', { status: 502, statusText: 'Bad Gateway' });

    expect(add).toHaveBeenCalledWith(
      jasmine.objectContaining({
        severity: 'error',
        detail: '發生未預期的錯誤。(An unexpected error occurred.)',
      }),
    );
  });

  it('does not toast on validation errors (400) so forms keep handling them', () => {
    const add = spyOn(messages, 'add');

    http.get('/api/app-roles').subscribe({ error: () => {} });

    const req = httpMock.expectOne('/api/app-roles');
    req.flush({ errors: { userId: ['required'] } }, { status: 400, statusText: 'Bad Request' });

    expect(add).not.toHaveBeenCalled();
  });
});
