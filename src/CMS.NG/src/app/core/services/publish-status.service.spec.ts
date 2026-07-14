import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PublishStatusService } from './publish-status.service';
import { PublishStatus, PublishStatusRequest } from '@core/models/publish-status.model';
import { environment } from '@env/environment';

describe('PublishStatusService', () => {
  let service: PublishStatusService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/publish-statuses`;

  const sample: PublishStatus = {
    pkid: 2,
    description: '已發布',
    isDraft: false,
    isPublished: true,
    isDiscontinued: false,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PublishStatusService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PublishStatusService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll issues GET /publish-statuses', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query POSTs the filter to /publish-statuses/query', () => {
    service.query({ keyword: '發布', isPublished: true }).subscribe((rows) =>
      expect(rows.length).toBe(1),
    );
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '發布', isPublished: true });
    req.flush([sample]);
  });

  it('getById requests the numeric pkid in the URL', () => {
    service.getById(2).subscribe((row) => expect(row.pkid).toBe(2));
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create POSTs the request body', () => {
    const request: PublishStatusRequest = {
      pkid: 4,
      description: '審核中',
      isDraft: true,
      isPublished: false,
      isDiscontinued: false,
    };
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, pkid: 4 });
  });

  it('update issues PUT with the request body', () => {
    const request: PublishStatusRequest = {
      pkid: 2,
      description: '已發布',
      isDraft: false,
      isPublished: true,
      isDiscontinued: false,
    };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(sample);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(2).subscribe();
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
