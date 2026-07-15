import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of } from 'rxjs';

import { CourseForm } from './course-form';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
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

function setup(routeId: string | null) {
  const serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', [
    'getById',
    'create',
    'update',
  ]);
  serviceSpy.getById.and.returnValue(of(COURSE));
  serviceSpy.create.and.returnValue(of({ ...COURSE, pkid: 5 }));
  serviceSpy.update.and.returnValue(of(COURSE));

  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', [
    'getPartners',
    'getCourseGroups',
    'getPublishStatuses',
  ]);
  lookupSpy.getPartners.and.returnValue(of([{ value: '1', label: '台灣微軟' }, { value: '2', label: '甲骨文' }]));
  lookupSpy.getCourseGroups.and.returnValue(of([{ value: '1', label: '雲端' }]));
  lookupSpy.getPublishStatuses.and.returnValue(of([{ value: '1', label: '草稿' }, { value: '2', label: '已發布' }]));

  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [CourseForm],
    providers: [
      provideNoopAnimations(),
      MessageService,
      { provide: CourseService, useValue: serviceSpy },
      { provide: LookupService, useValue: lookupSpy },
      { provide: Router, useValue: routerSpy },
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) } },
      },
    ],
  });

  const fixture: ComponentFixture<CourseForm> = TestBed.createComponent(CourseForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, routerSpy };
}

/** Asserts the action toolbar is rendered pinned (sticky) with Save + Cancel present. */
function expectStickyToolbarWithActions(fixture: ComponentFixture<CourseForm>) {
  const host: HTMLElement = fixture.nativeElement;

  const toolbar = host.querySelector<HTMLElement>('.page-toolbar');
  expect(toolbar).withContext('action toolbar renders').not.toBeNull();

  // Pinned/stuck styling comes from the component stylesheet.
  const style = getComputedStyle(toolbar!);
  expect(style.position).withContext('toolbar is sticky').toBe('sticky');
  expect(style.top).withContext('toolbar pins to the top').toBe('0px');

  // Save + Cancel remain in the toolbar.
  const buttons = toolbar!.querySelectorAll('p-button');
  expect(buttons.length).withContext('Save + Cancel buttons present').toBe(2);
  expect(toolbar!.textContent).toContain('儲存');
  expect(toolbar!.textContent).toContain('取消');
}

describe('CourseForm (add mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('starts in add mode with no pkid and loads lookups', () => {
    const { component } = setup(null);
    expect(component['isEdit']()).toBeFalse();
    expect(component['pkid']()).toBeNull();
    expect(component['partnerOptions']().length).toBe(2);
  });

  it('renders the sticky action toolbar with Save + Cancel', () => {
    const { fixture } = setup(null);
    expectStickyToolbarWithActions(fixture);
  });

  it('does not save an invalid (empty) form', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
  });

  it('creates the course and navigates to the DB-assigned pkid', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    component['form'].patchValue({
      title: 'K8s 入門',
      courseId: 'K8S-101',
      prodCourseId: 'PROD-K8S-101',
      friendlyUrl: 'k8s-101',
      displayOrder: 5,
      partnerPkid: 1,
      publishStatusPkid: 1,
      scheduleOn: new Date(2026, 2, 1),
      scheduleOff: new Date(2036, 2, 1),
      hour: 16,
      listPrice: 20000,
      learningCredit: 5,
      canRepeat: true,
    });

    component.save();

    expect(serviceSpy.create).toHaveBeenCalledWith(
      jasmine.objectContaining({
        pkid: 0,
        courseId: 'K8S-101',
        partnerPkid: 1,
        scheduleOn: '2026-03-01',
        scheduleOff: '2036-03-01',
        canRepeat: true,
      }),
    );
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses', 5]);
  });
});

describe('CourseForm (edit mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the course and patches the form (dates parsed to Date)', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBeTrue();
    expect(component['pkid']()).toBe(2);
    expect(component['form'].controls.title.value).toBe('AWS 架構師');
    expect(component['form'].controls.scheduleOn.value instanceof Date).toBeTrue();
  });

  it('renders the sticky action toolbar with Save + Cancel', () => {
    const { fixture } = setup('2');
    expectStickyToolbarWithActions(fixture);
  });

  it('updates the course on save (pkid carried from the route)', () => {
    const { component, serviceSpy, routerSpy } = setup('2');
    component['form'].controls.title.setValue('AWS 架構師（改版）');

    component.save();

    expect(serviceSpy.update).toHaveBeenCalledWith(
      jasmine.objectContaining({ pkid: 2, title: 'AWS 架構師（改版）' }),
    );
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses', 2]);
  });
});
