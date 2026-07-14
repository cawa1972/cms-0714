import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CourseGroupService } from './course-group.service';
import { CourseGroup, CourseGroupRequest } from '@core/models/course-group.model';
import { environment } from '@env/environment';

describe('CourseGroupService', () => {
  let service: CourseGroupService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/course-groups`;

  const sample: CourseGroup = {
    pkid: 2,
    description: '雲端運算',
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CourseGroupService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseGroupService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll issues GET /course-groups', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query POSTs the filter to /course-groups/query', () => {
    service.query({ keyword: '雲端' }).subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '雲端' });
    req.flush([sample]);
  });

  it('getById requests the numeric pkid in the URL', () => {
    service.getById(2).subscribe((row) => expect(row.pkid).toBe(2));
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create POSTs the request body', () => {
    const request: CourseGroupRequest = { pkid: 0, description: '數據分析' };
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ pkid: 4, description: '數據分析' });
  });

  it('update issues PUT with the request body', () => {
    const request: CourseGroupRequest = { pkid: 2, description: '雲端與維運' };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush({ pkid: 2, description: '雲端與維運' });
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(2).subscribe();
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
