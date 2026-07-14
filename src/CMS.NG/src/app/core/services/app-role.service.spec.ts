import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AppRoleService } from './app-role.service';
import { AppRole, AppRoleRequest } from '@core/models/app-role.model';
import { environment } from '@env/environment';

describe('AppRoleService', () => {
  let service: AppRoleService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/app-roles`;

  const sample: AppRole = {
    pkid: 1,
    roleId: 'Admin',
    roleName: 'Administrator',
    permissionLevel: 1,
    description: '系統管理員',
    userCount: 3,
    userIds: ['helen'],
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AppRoleService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AppRoleService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll issues GET /app-roles', () => {
    service.getAll().subscribe((roles) => expect(roles.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query POSTs the filter to /app-roles/query', () => {
    service.query({ keyword: 'admin', permissionLevel: 1 }).subscribe((roles) =>
      expect(roles.length).toBe(1),
    );
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'admin', permissionLevel: 1 });
    req.flush([sample]);
  });

  it('getById encodes the role id in the URL', () => {
    service.getById('a/b').subscribe((role) => expect(role.roleId).toBe('Admin'));
    const req = httpMock.expectOne(`${base}/a%2Fb`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create POSTs the request body', () => {
    const request: AppRoleRequest = {
      roleId: 'Editor',
      roleName: 'Editor',
      permissionLevel: 50,
      description: '編輯者',
      userIds: ['helen'],
    };
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, roleId: 'Editor' });
  });

  it('update issues PUT with the request body', () => {
    const request: AppRoleRequest = {
      roleId: 'Admin',
      roleName: 'Administrator',
      permissionLevel: 1,
      description: '系統管理員',
      userIds: [],
    };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sample);
  });

  it('delete issues DELETE with encoded id', () => {
    service.delete('Admin').subscribe();
    const req = httpMock.expectOne(`${base}/Admin`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
