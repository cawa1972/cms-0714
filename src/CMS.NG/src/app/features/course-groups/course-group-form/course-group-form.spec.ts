import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { CourseGroupForm } from './course-group-form';
import { CourseGroupService } from '@core/services/course-group.service';
import { CourseGroup } from '@core/models/course-group.model';

const GROUP: CourseGroup = { pkid: 2, description: '雲端運算' };

function setup(routeId: string | null) {
  const serviceSpy = jasmine.createSpyObj<CourseGroupService>('CourseGroupService', [
    'getById',
    'create',
    'update',
  ]);
  serviceSpy.getById.and.returnValue(of(GROUP));
  serviceSpy.create.and.returnValue(of({ pkid: 4, description: '數據分析' }));
  serviceSpy.update.and.returnValue(of(GROUP));
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [CourseGroupForm],
    providers: [
      provideNoopAnimations(),
      MessageService,
      { provide: CourseGroupService, useValue: serviceSpy },
      { provide: Router, useValue: routerSpy },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) },
        },
      },
    ],
  });

  const fixture: ComponentFixture<CourseGroupForm> = TestBed.createComponent(CourseGroupForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, routerSpy };
}

describe('CourseGroupForm (add mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('starts in add mode with no pkid', () => {
    const { component } = setup(null);
    expect(component['isEdit']()).toBeFalse();
    expect(component['pkid']()).toBeNull();
  });

  it('does not save an invalid (empty) form', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
  });

  it('creates the group and navigates on valid save', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    component['form'].setValue({ description: '數據分析' });

    component.save();

    expect(serviceSpy.create).toHaveBeenCalledWith({ pkid: 0, description: '數據分析' });
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/course-groups', 4]);
  });

  it('surfaces an error without navigating on save failure', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    serviceSpy.create.and.returnValue(throwError(() => new Error('boom')));
    component['form'].setValue({ description: '失敗案例' });

    component.save();

    expect(routerSpy.navigate).not.toHaveBeenCalled();
    expect(component['saving']()).toBeFalse();
  });
});

describe('CourseGroupForm (edit mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the group and patches the form', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBeTrue();
    expect(component['pkid']()).toBe(2);
    expect(component['form'].controls.description.value).toBe('雲端運算');
  });

  it('updates the group on save (pkid carried from loaded record)', () => {
    const { component, serviceSpy, routerSpy } = setup('2');
    component['form'].controls.description.setValue('雲端與維運');

    component.save();

    expect(serviceSpy.update).toHaveBeenCalledWith({ pkid: 2, description: '雲端與維運' });
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/course-groups', 2]);
  });
});
