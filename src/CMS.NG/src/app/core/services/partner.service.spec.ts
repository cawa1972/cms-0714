import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { PartnerService } from './partner.service';
import { Partner, PartnerRequest } from '@core/models/partner.model';
import { environment } from '@env/environment';

describe('PartnerService', () => {
  let service: PartnerService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/partners`;

  const sample: Partner = {
    pkid: 2,
    name: '甲骨文',
    appKey: 'ORACLE',
    nameOnPartnerMenu: '甲骨文原廠課程',
    nameOnCourseDetailPage: '甲骨文',
    displayOrder: 2,
    imageFilename: null,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [PartnerService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(PartnerService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll issues GET /partners', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([sample]);
  });

  it('query POSTs the filter to /partners/query', () => {
    service.query({ keyword: '甲骨文' }).subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: '甲骨文' });
    req.flush([sample]);
  });

  it('getById requests the numeric pkid in the URL', () => {
    service.getById(2).subscribe((row) => expect(row.pkid).toBe(2));
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('GET');
    req.flush(sample);
  });

  it('create POSTs the request body', () => {
    const request: PartnerRequest = {
      pkid: 0,
      name: '紅帽',
      appKey: 'REDHAT',
      nameOnPartnerMenu: '紅帽 Linux 課程',
      nameOnCourseDetailPage: '紅帽',
      displayOrder: 4,
      imageFilename: 'redhat.png',
    };
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...sample, pkid: 4 });
  });

  it('update issues PUT with the request body', () => {
    const request: PartnerRequest = {
      pkid: 2,
      name: '甲骨文',
      appKey: 'ORACLE',
      nameOnPartnerMenu: '甲骨文原廠課程',
      nameOnCourseDetailPage: '甲骨文',
      displayOrder: 2,
      imageFilename: null,
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
