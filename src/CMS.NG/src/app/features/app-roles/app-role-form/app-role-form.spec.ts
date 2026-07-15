import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AppRoleForm } from './app-role-form';
import { AppRoleService } from '@core/services/app-role.service';
import { LookupService } from '@core/services/lookup.service';
import { AppRole, LookupItem } from '@core/models/app-role.model';

const ROLE: AppRole = {
  pkid: 1,
  roleId: 'Admin',
  roleName: 'Administrator',
  permissionLevel: 1,
  description: '系統管理員',
  userCount: 1,
  userIds: ['helen'],
};

const USERS: LookupItem[] = [
  { value: 'helen', label: 'helen (helen)' },
  { value: 'miles', label: 'Miles Sun (miles)' },
];

function setup(routeId: string | null) {
  const serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', [
    'getById',
    'create',
    'update',
  ]);
  serviceSpy.getById.and.returnValue(of(ROLE));
  serviceSpy.create.and.returnValue(of(ROLE));
  serviceSpy.update.and.returnValue(of(ROLE));
  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppUsers']);
  lookupSpy.getAppUsers.and.returnValue(of(USERS));
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);

  TestBed.configureTestingModule({
    imports: [AppRoleForm],
    providers: [
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      MessageService,
      { provide: AppRoleService, useValue: serviceSpy },
      { provide: LookupService, useValue: lookupSpy },
      { provide: Router, useValue: routerSpy },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) },
        },
      },
    ],
  });

  const fixture: ComponentFixture<AppRoleForm> = TestBed.createComponent(AppRoleForm);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, serviceSpy, lookupSpy, routerSpy };
}

describe('AppRoleForm (add mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('starts in add mode with default permission level and loads user options', () => {
    const { component } = setup(null);
    expect(component['isEdit']()).toBeFalse();
    expect(component['form'].controls.permissionLevel.value).toBe(100);
    expect(component['userOptions']().length).toBe(2);
    expect(component['form'].controls.roleId.disabled).toBeFalse();
  });

  it('does not save an invalid (empty) form', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
  });

  it('creates the role and navigates on valid save', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    component['form'].setValue({
      roleId: 'Editor',
      roleName: 'Editor',
      permissionLevel: 50,
      description: '編輯者',
      userIds: ['helen'],
    });

    component.save();

    expect(serviceSpy.create).toHaveBeenCalledWith({
      roleId: 'Editor',
      roleName: 'Editor',
      permissionLevel: 50,
      description: '編輯者',
      userIds: ['helen'],
    });
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-roles', 'Editor']);
  });

  it('reports a 409 conflict without navigating', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    serviceSpy.create.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );
    component['form'].setValue({
      roleId: 'Admin',
      roleName: 'Dup',
      permissionLevel: 1,
      description: 'x',
      userIds: [],
    });

    component.save();

    expect(routerSpy.navigate).not.toHaveBeenCalled();
    expect(component['saving']()).toBeFalse();
  });
});

describe('AppRoleForm (edit mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the role, patches the form, and disables roleId', () => {
    const { component, serviceSpy } = setup('Admin');
    expect(serviceSpy.getById).toHaveBeenCalledWith('Admin');
    expect(component['isEdit']()).toBeTrue();
    expect(component['form'].controls.roleName.value).toBe('Administrator');
    expect(component['form'].controls.roleId.disabled).toBeTrue();
    expect(component['form'].controls.userIds.value).toEqual(['helen']);
  });

  it('updates the role on save (roleId included from disabled control)', () => {
    const { component, serviceSpy, routerSpy } = setup('Admin');
    component['form'].controls.roleName.setValue('General Admin');

    component.save();

    expect(serviceSpy.update).toHaveBeenCalledWith(
      jasmine.objectContaining({ roleId: 'Admin', roleName: 'General Admin' }),
    );
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-roles', 'Admin']);
  });
});
