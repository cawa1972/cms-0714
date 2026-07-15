import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { Confirmation, ConfirmationService, MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AppUserForm } from './app-user-form';
import { AppUserService } from '@core/services/app-user.service';
import { AuthService } from '@core/services/auth.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser } from '@core/models/app-user.model';
import { LookupItem } from '@core/models/app-role.model';

const USER: AppUser = {
  pkid: 1,
  userId: 'helen',
  userName: 'Helen Chen',
  isActive: true,
  passwordUpdatedTime: '2026-01-01T00:00:00',
  roleCount: 1,
  roleIds: ['Admin'],
};

const ROLES: LookupItem[] = [
  { value: 'Admin', label: 'Administrator (Admin)' },
  { value: 'Editor', label: 'Editor (Editor)' },
];

function setup(routeId: string | null, callerRoles: string[] = ['Admin']) {
  const serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', [
    'getById',
    'create',
    'update',
    'resetPassword',
  ]);
  serviceSpy.getById.and.returnValue(of(USER));
  serviceSpy.create.and.returnValue(of(USER));
  serviceSpy.update.and.returnValue(of(USER));
  serviceSpy.resetPassword.and.returnValue(of(void 0));
  const lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppRoles']);
  lookupSpy.getAppRoles.and.returnValue(of(ROLES));
  const routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
  const confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);
  const mockAuth = { hasRole: (role: string) => callerRoles.includes(role) };

  TestBed.configureTestingModule({
    imports: [AppUserForm],
    providers: [
      provideNoopAnimations(),
      provideHttpClient(),
      provideHttpClientTesting(),
      MessageService,
      { provide: AppUserService, useValue: serviceSpy },
      { provide: AuthService, useValue: mockAuth },
      { provide: LookupService, useValue: lookupSpy },
      { provide: Router, useValue: routerSpy },
      { provide: ConfirmationService, useValue: confirmSpy },
      {
        provide: ActivatedRoute,
        useValue: {
          snapshot: { paramMap: convertToParamMap(routeId ? { id: routeId } : {}) },
        },
      },
    ],
  });

  const fixture: ComponentFixture<AppUserForm> = TestBed.createComponent(AppUserForm);
  fixture.detectChanges();
  return {
    fixture,
    component: fixture.componentInstance,
    serviceSpy,
    lookupSpy,
    routerSpy,
    confirmSpy,
  };
}

describe('AppUserForm (add mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('starts in add mode with isActive defaulted and loads role options', () => {
    const { component } = setup(null);
    expect(component['isEdit']()).toBeFalse();
    expect(component['form'].controls.isActive.value).toBeTrue();
    expect(component['roleOptions']().length).toBe(2);
    expect(component['form'].controls.userId.disabled).toBeFalse();
  });

  it('does not save an invalid (empty) form', () => {
    const { component, serviceSpy } = setup(null);
    component.save();
    expect(serviceSpy.create).not.toHaveBeenCalled();
  });

  it('creates the user and navigates on valid save (no password in payload)', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    component['form'].setValue({
      userId: 'jenny',
      userName: 'Jenny Tsao',
      isActive: true,
      roleIds: ['Admin'],
    });

    component.save();

    expect(serviceSpy.create).toHaveBeenCalledWith({
      userId: 'jenny',
      userName: 'Jenny Tsao',
      isActive: true,
      roleIds: ['Admin'],
    });
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-users', 'jenny']);
  });

  it('reports a 409 conflict without navigating', () => {
    const { component, serviceSpy, routerSpy } = setup(null);
    serviceSpy.create.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 409 })),
    );
    component['form'].setValue({
      userId: 'helen',
      userName: 'Dup',
      isActive: true,
      roleIds: [],
    });

    component.save();

    expect(routerSpy.navigate).not.toHaveBeenCalled();
    expect(component['saving']()).toBeFalse();
  });
});

describe('AppUserForm (edit mode)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('loads the user, patches the form, and disables userId', () => {
    const { component, serviceSpy } = setup('helen');
    expect(serviceSpy.getById).toHaveBeenCalledWith('helen');
    expect(component['isEdit']()).toBeTrue();
    expect(component['form'].controls.userName.value).toBe('Helen Chen');
    expect(component['form'].controls.userId.disabled).toBeTrue();
    expect(component['form'].controls.roleIds.value).toEqual(['Admin']);
  });

  it('updates the user on save (userId included from disabled control)', () => {
    const { component, serviceSpy, routerSpy } = setup('helen');
    component['form'].controls.userName.setValue('Helen C.');

    component.save();

    expect(serviceSpy.update).toHaveBeenCalledWith(
      jasmine.objectContaining({ userId: 'helen', userName: 'Helen C.' }),
    );
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-users', 'helen']);
  });
});

describe('AppUserForm (reset password to default)', () => {
  afterEach(() => TestBed.resetTestingModule());

  it('shows the reset-password button for Admin users in edit mode', () => {
    const { fixture } = setup('helen', ['Admin']);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('重設密碼');
  });

  it('hides the reset-password button for non-Admin users', () => {
    const { fixture } = setup('helen', ['Editor']);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('重設密碼');
  });

  it('hides the reset-password button in add mode even for Admins', () => {
    const { fixture } = setup(null, ['Admin']);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).not.toContain('重設密碼');
  });

  it('resets the password (sending only the UserId) after the confirm is accepted', () => {
    const { component, serviceSpy, confirmSpy } = setup('helen', ['Admin']);
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.resetPassword(new Event('click'));

    expect(serviceSpy.resetPassword).toHaveBeenCalledWith('helen');
  });

  it('does not reset when the confirm is not accepted', () => {
    const { component, serviceSpy, confirmSpy } = setup('helen', ['Admin']);
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.reject?.();
      return confirmSpy;
    });

    component.resetPassword(new Event('click'));

    expect(serviceSpy.resetPassword).not.toHaveBeenCalled();
  });
});
