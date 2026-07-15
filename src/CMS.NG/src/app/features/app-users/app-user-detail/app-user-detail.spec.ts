import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of } from 'rxjs';

import { AppUserDetail } from './app-user-detail';
import { AppUserService } from '@core/services/app-user.service';
import { LookupService } from '@core/services/lookup.service';
import { AppUser } from '@core/models/app-user.model';
import { LookupItem } from '@core/models/app-role.model';

const USER: AppUser = {
  pkid: 1,
  userId: 'helen',
  userName: 'Helen Chen',
  isActive: true,
  passwordUpdatedTime: '2026-01-01T00:00:00',
  roleCount: 2,
  roleIds: ['Admin', 'Editor'],
};

const ROLES: LookupItem[] = [
  { value: 'Admin', label: 'Administrator (Admin)' },
  { value: 'Editor', label: 'Editor (Editor)' },
  { value: 'User', label: 'User (User)' },
];

describe('AppUserDetail', () => {
  let fixture: ComponentFixture<AppUserDetail>;
  let component: AppUserDetail;
  let serviceSpy: jasmine.SpyObj<AppUserService>;
  let lookupSpy: jasmine.SpyObj<LookupService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    serviceSpy = jasmine.createSpyObj<AppUserService>('AppUserService', ['getById', 'resetPassword']);
    serviceSpy.getById.and.returnValue(of(USER));
    serviceSpy.resetPassword.and.returnValue(of(void 0));
    lookupSpy = jasmine.createSpyObj<LookupService>('LookupService', ['getAppRoles']);
    lookupSpy.getAppRoles.and.returnValue(of(ROLES));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [AppUserDetail],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: AppUserService, useValue: serviceSpy },
        { provide: LookupService, useValue: lookupSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'helen' }) } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppUserDetail);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the user by id from the route', () => {
    expect(serviceSpy.getById).toHaveBeenCalledWith('helen');
    expect(component['user']()?.userName).toBe('Helen Chen');
  });

  it('maps role ids to their lookup labels', () => {
    expect(component['roleLabels']()).toEqual(['Administrator (Admin)', 'Editor (Editor)']);
  });

  it('resetPassword calls the service when confirmed', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.resetPassword(new Event('click'));

    expect(serviceSpy.resetPassword).toHaveBeenCalledWith('helen');
  });

  it('edit navigates to the edit form', () => {
    component.edit();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-users', 'helen', 'edit']);
  });

  it('back navigates to the list', () => {
    component.back();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-users']);
  });
});
