import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { PublishStatusForm } from './publish-status-form';
import { PublishStatusService } from '@core/services/publish-status.service';
import { PublishStatus } from '@core/models/publish-status.model';

const STATUS: PublishStatus = {
  pkid: 2,
  description: '已發布',
  isDraft: false,
  isPublished: true,
  isDiscontinued: false,
};

function setup(routeId: string | null) {
  const serviceSpy = jasmine.createSpyObj<PublishStatusService>('PublishStatusService', [
    'getById',
    'create',
    'update',
  ]);
  serviceSpy.getById.and.returnValue(of(STATUS));
  serviceSpy.create.and.returnValue(of(STATUS));
  serviceSpy.update.and.returnValue(of(STATUS));
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [PublishStatusForm],
    providers: [
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      MessageService,
      { provide: PublishStatusService, useValue: serviceSpy },
      { provide: Router, useValue: routerSpy },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) },
        },
      },
    ],
  });

  const fixture: ComponentFixture<PublishStatusForm> = TestBed.createComponent(PublishStatusForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, routerSpy };
}

describe('PublishStatusForm (add mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('starts in add mode with the pkid field enabled', () => {
    const { component } = setup(null);
    expect(component['isEdit']()).toBeFalse();
    expect(component['form'].controls.pkid.disabled).toBeFalse();
  });

  it('does not save an invalid (empty) form', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
  });

  it('creates the status and navigates on valid save', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    component['form'].setValue({
      pkid: 4,
      description: '審核中',
      isDraft: true,
      isPublished: false,
      isDiscontinued: false,
    });

    component.save();

    expect(serviceSpy.create).toHaveBeenCalledWith({
      pkid: 4,
      description: '審核中',
      isDraft: true,
      isPublished: false,
      isDiscontinued: false,
    });
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/publish-statuses', 4]);
  });

  it('reports a 409 conflict without navigating', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    serviceSpy.create.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    component['form'].setValue({
      pkid: 1,
      description: '重複',
      isDraft: true,
      isPublished: false,
      isDiscontinued: false,
    });

    component.save();

    expect(routerSpy.navigate).not.toHaveBeenCalled();
    expect(component['saving']()).toBeFalse();
  });
});

describe('PublishStatusForm (edit mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the status, patches the form, and disables pkid', () => {
    const { component, serviceSpy } = setup('2');
    expect(serviceSpy.getById).toHaveBeenCalledWith(2);
    expect(component['isEdit']()).toBeTrue();
    expect(component['form'].controls.description.value).toBe('已發布');
    expect(component['form'].controls.pkid.disabled).toBeTrue();
    expect(component['form'].controls.isPublished.value).toBeTrue();
  });

  it('updates the status on save (pkid included from disabled control)', () => {
    const { component, serviceSpy, routerSpy } = setup('2');
    component['form'].controls.description.setValue('已上線');

    component.save();

    expect(serviceSpy.update).toHaveBeenCalledWith(
      jasmine.objectContaining({ pkid: 2, description: '已上線' }),
    );
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/publish-statuses', 2]);
  });
});
