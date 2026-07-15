import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

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
};

function setup(getByIdReturn = of(COURSE)) {
  const serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['getById']);
  serviceSpy.getById.and.returnValue(getByIdReturn);
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [CourseDetail],
    providers: [
      provideNoopAnimations(),
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
});
