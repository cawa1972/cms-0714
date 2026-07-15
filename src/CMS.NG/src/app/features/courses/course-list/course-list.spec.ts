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
    serviceSpy = jasmine.createSpyObj<CourseService>('CourseService', ['query', 'delete', 'update']);
    serviceSpy.query.and.returnValue(of(COURSES));
    serviceSpy.delete.and.returnValue(of(void 0));
    serviceSpy.update.and.callFake((req) => of({ ...COURSES[0], ...req } as Course));

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

  describe('inline cell editing', () => {
    // Returns the <td> cells of the given data row (0-based).
    function cellsOfRow(rowIndex: number): HTMLTableCellElement[] {
      const rows = fixture.nativeElement.querySelectorAll('.p-datatable-tbody tr');
      return Array.from((rows[rowIndex] as HTMLTableRowElement).querySelectorAll('td'));
    }

    // Column order in the body row (matches the header).
    const COL = {
      pkid: 0,
      displayOrder: 1,
      courseId: 2,
      prodCourseId: 3,
      title: 4,
      partnerName: 5,
      courseGroup: 6,
      publishStatus: 7,
      scheduleOn: 8,
      scheduleOff: 9,
      hour: 10,
      listPrice: 11,
      learningCredit: 12,
      canRepeat: 13,
    };

    it('enters edit mode on double-click, not on single-click', () => {
      const titleCell = cellsOfRow(0)[COL.title];

      // Single click must NOT trigger editing.
      titleCell.dispatchEvent(new MouseEvent('click', { bubbles: true }));
      fixture.detectChanges();
      expect(component['editingField']()).toBeNull();
      expect(titleCell.querySelector('input')).toBeNull();

      // Double-click enters edit mode and renders the editor.
      titleCell.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
      fixture.detectChanges();
      expect(component['editingPkid']()).toBe(COURSES[0].pkid);
      expect(component['editingField']()).toBe('title');
      expect(titleCell.querySelector('input')).not.toBeNull();
    });

    it('does not enter edit mode when a read-only column is double-clicked', () => {
      for (const col of [COL.pkid, COL.partnerName, COL.courseGroup]) {
        const cell = cellsOfRow(0)[col];
        cell.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
        fixture.detectChanges();
        expect(component['editingField']()).toBeNull();
        expect(cell.querySelector('input')).toBeNull();
        expect(cell.classList).not.toContain('editable');
      }
    });

    it('persists via the update endpoint on blur when the value changed', () => {
      component.startEdit(COURSES[0], 'title');
      component['editValue'] = 'Azure 進階';
      component.commitEdit(COURSES[0]); // simulates blur

      expect(serviceSpy.update).toHaveBeenCalledTimes(1);
      const sent = serviceSpy.update.calls.mostRecent().args[0];
      expect(sent.pkid).toBe(COURSES[0].pkid);
      expect(sent.title).toBe('Azure 進階');
      // Editor closes and the row reflects the saved value.
      expect(component['editingField']()).toBeNull();
      expect(component['courses']()[0].title).toBe('Azure 進階');
    });

    it('trims text and does NOT call update when the value is unchanged', () => {
      component.startEdit(COURSES[0], 'title');
      component['editValue'] = `  ${COURSES[0].title}  `; // same after trim
      component.commitEdit(COURSES[0]);

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component['editingField']()).toBeNull();
    });

    it('blocks clearing a required text field and keeps the cell in edit mode', () => {
      component.startEdit(COURSES[0], 'title');
      component['editValue'] = '   ';
      component.commitEdit(COURSES[0]);

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component['editError']()).toBeTruthy();
      expect(component['editingField']()).toBe('title'); // still editing
    });

    it('blocks negative numbers on numeric columns', () => {
      for (const field of ['hour', 'listPrice', 'learningCredit', 'displayOrder'] as const) {
        component.startEdit(COURSES[0], field);
        component['editValue'] = -1;
        component.commitEdit(COURSES[0]);

        expect(serviceSpy.update).not.toHaveBeenCalled();
        expect(component['editError']()).toBeTruthy();
        expect(component['editingField']()).toBe(field);
        component.cancelEdit();
      }
    });

    it('blocks numbers above each column max', () => {
      const overMax: Record<string, number> = {
        displayOrder: 10_000,
        hour: 10_000,
        listPrice: 10_000_000,
        learningCredit: 10_000,
      };
      for (const field of ['displayOrder', 'hour', 'listPrice', 'learningCredit'] as const) {
        component.startEdit(COURSES[0], field);
        component['editValue'] = overMax[field];
        component.commitEdit(COURSES[0]);

        expect(serviceSpy.update).not.toHaveBeenCalled();
        expect(component['editError']()).toBeTruthy();
        expect(component['editingField']()).toBe(field);
        component.cancelEdit();
      }
    });

    it('blocks an empty required numeric field', () => {
      component.startEdit(COURSES[0], 'hour');
      component['editValue'] = null;
      component.commitEdit(COURSES[0]);

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component['editError']()).toBeTruthy();
      expect(component['editingField']()).toBe('hour');
    });

    it('blocks an invalid date value', () => {
      component.startEdit(COURSES[0], 'scheduleOn');
      component['editValue'] = new Date('not-a-date');
      component.commitEdit(COURSES[0]);

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component['editError']()).toBeTruthy();
      expect(component['editingField']()).toBe('scheduleOn');
    });

    it('blocks 上架日期 later than 下架日期', () => {
      // Row 0 scheduleOff is 2036-01-01; pick an 上架日期 after it.
      component.startEdit(COURSES[0], 'scheduleOn');
      component['editValue'] = new Date(2037, 0, 1);
      component.commitEdit(COURSES[0]);

      expect(serviceSpy.update).not.toHaveBeenCalled();
      expect(component['editError']()).toBeTruthy();
      expect(component['editingField']()).toBe('scheduleOn');
    });

    it('accepts a valid 上架日期 on or before 下架日期', () => {
      component.startEdit(COURSES[0], 'scheduleOn');
      component['editValue'] = new Date(2027, 5, 30);
      component.commitEdit(COURSES[0]);

      expect(serviceSpy.update).toHaveBeenCalledTimes(1);
      expect(serviceSpy.update.calls.mostRecent().args[0].scheduleOn).toBe('2027-06-30');
    });

    it('commits a 上架狀態 dropdown change through the update endpoint', () => {
      component.startEdit(COURSES[0], 'publishStatusPkid');
      component['editValue'] = 2; // pick a different status
      component.commitEdit(COURSES[0]); // simulates (onChange)

      expect(serviceSpy.update).toHaveBeenCalledTimes(1);
      expect(serviceSpy.update.calls.mostRecent().args[0].publishStatusPkid).toBe(2);
      expect(component['editingField']()).toBeNull();
    });

    it('closes a panel editor on hide when idle, but not while an error is shown', () => {
      // Idle (no change picked) → hiding the panel closes the editor.
      component.startEdit(COURSES[0], 'publishStatusPkid');
      component.onEditorPanelHide(COURSES[0]);
      expect(component['editingField']()).toBeNull();

      // With a validation error showing, hiding must keep the cell in edit mode.
      component.startEdit(COURSES[0], 'scheduleOn');
      component['editValue'] = new Date('not-a-date');
      component.commitEdit(COURSES[0]);
      expect(component['editError']()).toBeTruthy();
      component.onEditorPanelHide(COURSES[0]);
      expect(component['editingField']()).toBe('scheduleOn');
    });

    it('reverts the cell and surfaces an error when the save fails', () => {
      serviceSpy.update.and.returnValue(throwError(() => new Error('save failed')));

      component.startEdit(COURSES[0], 'title');
      component['editValue'] = '壞掉了';
      component.commitEdit(COURSES[0]);

      // Row keeps its previous value; editor closes.
      expect(component['courses']()[0].title).toBe(COURSES[0].title);
      expect(component['editingField']()).toBeNull();
    });
  });
});
