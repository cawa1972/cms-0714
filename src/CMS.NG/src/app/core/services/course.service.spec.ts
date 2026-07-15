import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { CourseService } from './course.service';
import { Course, CourseRequest } from '@core/models/course.model';
import { environment } from '@env/environment';

const SAMPLE: Course = {
  pkid: 2,
  title: 'AWS 架構師',
  officialTitle: null,
  courseId: 'AWS-SAA',
  prodCourseId: 'PROD-AWS-SAA',
  friendlyUrl: 'aws-saa',
  displayOrder: 2,
  partnerPkid: 2,
  courseGroupPkid: null,
  publishStatusPkid: 2,
  scheduleOn: '2026-01-01',
  scheduleOff: '2036-01-01',
  hour: 8,
  listPrice: 12000,
  learningCredit: 3.5,
  material: null,
  objective: null,
  target: null,
  prerequisites: null,
  outline: null,
  towardCertOrExam: null,
  note: null,
  otherInfo: null,
  canRepeat: false,
  partnerName: '甲骨文',
  courseGroupDescription: null,
  publishStatusDescription: '已發布',
};

function newRequest(): CourseRequest {
  return {
    pkid: 0,
    title: 'K8s 入門',
    officialTitle: null,
    courseId: 'K8S-101',
    prodCourseId: 'PROD-K8S-101',
    friendlyUrl: 'k8s-101',
    displayOrder: 5,
    partnerPkid: 1,
    courseGroupPkid: null,
    publishStatusPkid: 1,
    scheduleOn: '2026-03-01',
    scheduleOff: '2036-03-01',
    hour: 16,
    listPrice: 20000,
    learningCredit: 5,
    material: null,
    objective: null,
    target: null,
    prerequisites: null,
    outline: null,
    towardCertOrExam: null,
    note: null,
    otherInfo: null,
    canRepeat: true,
  };
}

describe('CourseService', () => {
  let service: CourseService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/courses`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [CourseService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(CourseService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('getAll issues GET /courses', () => {
    service.getAll().subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('GET');
    req.flush([SAMPLE]);
  });

  it('query POSTs the filter to /courses/query', () => {
    service.query({ keyword: 'AWS' }).subscribe((rows) => expect(rows.length).toBe(1));
    const req = httpMock.expectOne(`${base}/query`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ keyword: 'AWS' });
    req.flush([SAMPLE]);
  });

  it('getById requests the numeric pkid in the URL', () => {
    service.getById(2).subscribe((row) => expect(row.pkid).toBe(2));
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('GET');
    req.flush(SAMPLE);
  });

  it('create POSTs the request body', () => {
    const request = newRequest();
    service.create(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({ ...SAMPLE, pkid: 4 });
  });

  it('update issues PUT with the request body', () => {
    const request = { ...newRequest(), pkid: 2 };
    service.update(request).subscribe();
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    req.flush(SAMPLE);
  });

  it('delete issues DELETE with the numeric pkid', () => {
    service.delete(2).subscribe();
    const req = httpMock.expectOne(`${base}/2`);
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
  });
});
