import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { CourseGroupList } from './course-group-list';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

const GROUPS: CourseGroup[] = [
  { pkid: 1, description: '程式設計' },
  { pkid: 2, description: '雲端運算' },
];

describe('CourseGroupList', () => {
  let fixture: ComponentFixture<CourseGroupList>;
  let component: CourseGroupList;
  let serviceSpy: jasmine.SpyObj<CourseGroupService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(GROUPS));
    serviceSpy.delete.and.returnValue(of(void 0));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [CourseGroupList],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: CourseGroupService, useValue: serviceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CourseGroupList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates and loads groups on init', () => {
    expect(component).toBeTruthy();
    expect(serviceSpy.query).toHaveBeenCalledTimes(1);
    expect(component['groups']().length).toBe(2);
  });

  it('applyFilters persists filters and reloads', () => {
    component['filters'] = { keyword: '雲端' };
    component.applyFilters();

    expect(JSON.parse(sessionStorage.getItem('course-group-list-filters')!)).toEqual({
      keyword: '雲端',
    });
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(component['drawerVisible']()).toBeFalse();
  });

  it('clearFilters resets and removes saved filters', () => {
    sessionStorage.setItem('course-group-list-filters', JSON.stringify({ keyword: 'x' }));
    component.clearFilters();

    expect(sessionStorage.getItem('course-group-list-filters')).toBeNull();
    expect(component['filters']).toEqual({ keyword: null });
  });

  it('add navigates to the new form', () => {
    component.add();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/course-groups/new']);
  });

  it('view and edit navigate with the pkid', () => {
    component.view(GROUPS[0]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/course-groups', 1]);
    component.edit(GROUPS[1]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/course-groups', 2, 'edit']);
  });

  it('remove deletes when the confirmation is accepted', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.remove(GROUPS[0], new Event('click'));

    expect(serviceSpy.delete).toHaveBeenCalledWith(1);
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error when loading fails', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBeFalse();
  });
});
