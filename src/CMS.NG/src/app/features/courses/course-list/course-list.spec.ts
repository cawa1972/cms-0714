import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { CourseList } from './course-list';
import { CourseService } from '@core/services/course.service';
import { LookupService } from '@core/services/lookup.service';
import { Course } from '@core/models/course.model';

function course(pkid: number, courseId: string, title: string): Course {
  return {
    pkid,
    title,
    officialTitle: null,
    courseId,
    prodCourseId: `PROD-${courseId}`,
    friendlyUrl: courseId.toLowerCase(),
    displayOrder: pkid,
    partnerPkid: 1,
    courseGroupPkid: null,
    publishStatusPkid: 1,
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
    partnerName: '台灣微軟',
    courseGroupDescription: null,
    publishStatusDescription: '已發布',
  };
}

const COURSES: Course[] = [course(1, 'AZ-900', 'Azure 基礎'), course(2, 'AWS-SAA', 'AWS 架構師')];

describe('CourseList', () => {
  let fixture: ComponentFixture<CourseList>;
  let component: CourseList;
  let serviceSpy: jasmine.SpyObj<CourseService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(COURSES));
    serviceSpy.delete.and.returnValue(of(void 0));

    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', [
      'getPartners',
      'getCourseGroups',
      'getPublishStatuses',
    ]);
    lookupSpy.getPartners.and.returnValue(of([{ value: '1', label: '台灣微軟' }]));
    lookupSpy.getCourseGroups.and.returnValue(of([{ value: '1', label: '雲端' }]));
    lookupSpy.getPublishStatuses.and.returnValue(of([{ value: '1', label: '已發布' }]));

    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [CourseList],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: CourseService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates, loads courses, and maps lookup options on init', () => {
    expect(component).toBeTruthy();
    expect(serviceSpy.query).toHaveBeenCalledTimes(1);
    expect(component['courses']().length).toBe(2);
    expect(component['partnerOptions']()).toEqual([{ value: 1, label: '台灣微軟' }]);
    expect(component['publishStatusOptions']()).toEqual([{ value: 1, label: '已發布' }]);
  });

  it('applyFilters serializes date ranges to ISO and reloads', () => {
    component['scheduleOnFrom'] = new Date(2026, 0, 15);
    component.applyFilters();

    expect(component['filters'].scheduleOnFrom).toBe('2026-01-15');
    const saved = JSON.parse(sessionStorage.getItem('course-list-filters')!);
    expect(saved.scheduleOnFrom).toBe('2026-01-15');
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(component['drawerVisible']()).toBeFalse();
  });

  it('clearFilters resets filters and date pickers', () => {
    sessionStorage.setItem('course-list-filters', JSON.stringify({ keyword: 'x' }));
    component['scheduleOnFrom'] = new Date();
    component.clearFilters();

    expect(sessionStorage.getItem('course-list-filters')).toBeNull();
    expect(component['scheduleOnFrom']).toBeNull();
    expect(component['filters'].keyword).toBeNull();
  });

  it('add navigates to the new form', () => {
    component.add();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses/new']);
  });

  it('view and edit navigate with the pkid', () => {
    component.view(COURSES[0]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses', 1]);
    component.edit(COURSES[1]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/courses', 2, 'edit']);
  });

  it('remove deletes when the confirmation is accepted', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.remove(COURSES[0], new Event('click'));

    expect(serviceSpy.delete).toHaveBeenCalledWith(1);
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error when loading fails', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBeFalse();
  });
});
