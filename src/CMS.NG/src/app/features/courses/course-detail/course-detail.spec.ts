import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { HttpErrorResponse, HttpHeaders, HttpResponse, provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MessageService } from 'primeng/api';
import { Subject, of, throwError } from 'rxjs';

import { CourseDetail } from './course-detail';
import { CourseService } from '@core/services/course.service';
import { Course } from '@core/models/course.model';

const COURSE: Course = {
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
  publishStatusIsPublished: true,
};

function setup(getByIdReturn = of(COURSE)) {
  const serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', [
    'getById',
    'downloadFlyer',
  ]);
  serviceSpy.getById.and.returnValue(getByIdReturn);
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [CourseDetail],
    providers: [
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      MessageService,
      { provide: CourseService, useValue: serviceSpy },
      { provide: Router, useValue: routerSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap({ id: '2' }) } },
      },
    ],
  });

  const fixture: ComponentFixture<CourseDetail> = TestBed.createComponent(CourseDetail);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, routerSpy };
}

describe('CourseDetail', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the course by the route pkid', () => {
    const { component, serviceSpy } = setup();
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['course']()?.title).toBe('AWS 架構師');
  });

  it('edit navigates to the edit route', () => {
    const { component, routerSpy } = setup();
    component.edit();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses', 2, 'edit']);
  });

  it('back navigates to the list', () => {
    const { component, routerSpy } = setup();
    component.back();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses']);
  });

  it('redirects to the list when loading fails', () => {
    const { routerSpy } = setup(throwError(() => new Error('missing')));
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses']);
  });

  it('derives the QR code URL from the record pkid and courseId', () => {
    const { component } = setup();
    expect(component['qrCodeUrl']()).toBe('https://www.uuu.com.tw/Course/Show/2/AWS-SAA');
  });

  it('passes courseId as the QR code title', async () => {
    const { fixture } = setup();
    await fixture.whenStable();
    fixture.detectChanges();

    const titleEl: HTMLElement = fixture.nativeElement.querySelector('.qr-code-title');
    expect(titleEl.textContent?.trim()).toBe('AWS-SAA');
  });

  it('renders a downloadable QR code image for the course', async () => {
    const { fixture } = setup();
    await fixture.whenStable();
    fixture.detectChanges();

    const img: HTMLImageElement = fixture.nativeElement.querySelector('.qr-code-image');
    expect(img.src).toMatch(/^data:image\/png;base64,/);
  });

  // ---- Flyer download ----------------------------------------------------

  function pdfResponse(contentDisposition?: string): HttpResponse<Blob> {
    return new HttpResponse({
      body: new Blob(['%PDF-fake'], { type: 'application/pdf' }),
      headers: contentDisposition
        ? new HttpHeaders({ 'Content-Disposition': contentDisposition })
        : new HttpHeaders(),
    });
  }

  /** Captures the anchor download name saveBlob assigns (real DOM chain, no module spies). */
  function spyOnAnchorDownload(): { name: () => string | undefined } {
    spyOn(URL, 'createObjectURL').and.returnValue('blob:fake-url');
    spyOn(URL, 'revokeObjectURL');
    let captured: string | undefined;
    spyOn(HTMLAnchorElement.prototype, 'click').and.callFake(function (this: HTMLAnchorElement) {
      captured = this.download;
    });
    return { name: () => captured };
  }

  it('downloads the flyer under the server-provided RFC 5987 filename', () => {
    const { component, serviceSpy } = setup();
    const anchor = spyOnAnchorDownload();
    serviceSpy.downloadFlyer.and.returnValue(
      of(
        pdfResponse(
          "attachment; filename=course-AWS-SAA.pdf; filename*=UTF-8''%E8%AA%B2%E7%A8%8B%E7%B0%A1%E4%BB%8B-AWS.pdf",
        ),
      ),
    );

    component.downloadFlyer();

    expect(serviceSpy.downloadFlyer).toHaveBeenCalledWith(2);
    expect(anchor.name()).toBe('課程簡介-AWS.pdf');
    expect(component['downloadingFlyer']()).toBeFalse();
  });

  it('falls back to the locally-built filename when the header is absent', () => {
    const { component, serviceSpy } = setup();
    const anchor = spyOnAnchorDownload();
    serviceSpy.downloadFlyer.and.returnValue(of(pdfResponse()));

    component.downloadFlyer();

    expect(anchor.name()).toBe('課程簡介-AWS 架構師.pdf');
  });

  it('falls back to course-{pkid}.pdf when the title sanitizes to empty', () => {
    const { component, serviceSpy } = setup(of({ ...COURSE, title: ' . ' }));
    const anchor = spyOnAnchorDownload();
    serviceSpy.downloadFlyer.and.returnValue(of(pdfResponse()));

    component.downloadFlyer();

    expect(anchor.name()).toBe('course-2.pdf');
  });

  it('does nothing when no course is loaded', () => {
    const { component, serviceSpy } = setup(throwError(() => new Error('missing')));

    component.downloadFlyer();

    expect(serviceSpy.downloadFlyer).not.toHaveBeenCalled();
  });

  it('shows a busy state while downloading and guards against a second click', () => {
    const { component, serviceSpy } = setup();
    const pending = new Subject<HttpResponse<Blob>>();
    serviceSpy.downloadFlyer.and.returnValue(pending.asObservable());

    component.downloadFlyer();
    expect(component['downloadingFlyer']()).toBeTrue();

    component.downloadFlyer(); // second click while busy
    expect(serviceSpy.downloadFlyer).toHaveBeenCalledTimes(1);
  });

  it('toasts on 404', () => {
    const { component, serviceSpy } = setup();
    const messages = TestBed.inject(MessageService);
    const addSpy = spyOn(messages, 'add');
    serviceSpy.downloadFlyer.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 404 })),
    );

    component.downloadFlyer();

    expect(addSpy).toHaveBeenCalledWith(
      jasmine.objectContaining({ severity: 'error', detail: '找不到課程資料。' }),
    );
    expect(component['downloadingFlyer']()).toBeFalse();
  });

  it('stays silent on 5xx (the auth interceptor owns that toast)', () => {
    const { component, serviceSpy } = setup();
    const messages = TestBed.inject(MessageService);
    const addSpy = spyOn(messages, 'add');
    serviceSpy.downloadFlyer.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    component.downloadFlyer();

    expect(addSpy).not.toHaveBeenCalled();
    expect(component['downloadingFlyer']()).toBeFalse();
  });
});
