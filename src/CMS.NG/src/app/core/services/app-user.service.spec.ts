import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AppUserService } from './app-user.service';
import { AppUser, AppUserRequest } from '@core/models/app-user.model';
import { environment } from '@env/environment';

describe('AppUserService', () => {
  let service: AppUserService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/app-users`;

  const sample: AppUser = {
    pkid: 1,
    userId: 'helen',
    userName: 'Helen Chen',
    isActive: true,
    passwordUpdatedTime: '2026-01-01T00:00:00',
    roleCount: 2,
    roleIds: ['Admin'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AppUserService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppUserService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll issues GET /app-users', () => {
    service.getAll().subscribe((users) => expect(users.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query POSTs the filter to /app-users/query', () => {
    service.query({ keyword: 'helen', isActive: true }).subscribe((users) =>
      expect(users.length).toBe(1),
    );
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'helen', isActive: true });
    req.flush([sample]);
  });

  it('getById encodes the user id in the URL', () => {
    service.getById('a/b').subscribe((user) => expect(user.userId).toBe('helen'));
    const req = httpMock.expectOne(`${base}/a%2Fb`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create POSTs the request body (no password field)', () => {
    const request: AppUserRequest = {
      userId: 'jenny',
      userName: 'Jenny Tsao',
      isActive: true,
      roleIds: ['Admin'],
    };
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect('passwordHash' in req.request.body).toBeFalse();
    req.flush({ ...sample, userId: 'jenny' });
  });

  it('update issues PUT with the request body', () => {
    const request: AppUserRequest = {
      userId: 'helen',
      userName: 'Helen Chen',
      isActive: true,
      roleIds: [],
    };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sample);
  });

  it('delete issues DELETE with encoded id', () => {
    service.delete('helen').subscribe();
    const req = httpMock.expectOne(`${base}/helen`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });

  it('resetPassword POSTs to /{id}/reset-password with an encoded id', () => {
    service.resetPassword('a/b').subscribe();
    const req = httpMock.expectOne(`${base}/a%2Fb/reset-password`);
    expect(req.request.method).toBe('POST');
    req.flush(null);
  });
});
