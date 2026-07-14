import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ConfirmationService, MessageService, Confirmation } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { AppRoleList } from './app-role-list';
import { AppRoleService } from '@core/services/app-role.service';
import { AppRole } from '@core/models/app-role.model';

const ROLES: AppRole[] = [
  { pkid: 1, roleId: 'Admin', roleName: 'Administrator', permissionLevel: 1, description: '系統管理員', userCount: 3, userIds: [] },
  { pkid: 2, roleId: 'User', roleName: 'User', permissionLevel: 100, description: '一般使用者', userCount: 9, userIds: [] },
];

describe('AppRoleList', () => {
  let fixture: ComponentFixture<AppRoleList>;
  let component: AppRoleList;
  let serviceSpy: jasmine.SpyObj<AppRoleService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let confirmSpy: jasmine.SpyObj<ConfirmationService>;

  beforeEach(async () => {
    sessionStorage.clear();
    serviceSpy = jasmine.createSpyObj<AppRoleService>('AppRoleService', ['query', 'delete']);
    serviceSpy.query.and.returnValue(of(ROLES));
    serviceSpy.delete.and.returnValue(of(void 0));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    confirmSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    await TestBed.configureTestingModule({
      imports: [AppRoleList],
      providers: [
        provideNoopAnimations(),
        MessageService,
        { provide: AppRoleService, useValue: serviceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ConfirmationService, useValue: confirmSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AppRoleList);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates and loads roles on init', () => {
    expect(component).toBeTruthy();
    expect(serviceSpy.query).toHaveBeenCalledTimes(1);
    expect(component['roles']().length).toBe(2);
  });

  it('applyFilters persists filters and reloads', () => {
    component['filters'] = { keyword: 'admin', permissionLevel: 1 };
    component.applyFilters();

    expect(JSON.parse(sessionStorage.getItem('app-role-list-filters')!)).toEqual({
      keyword: 'admin',
      permissionLevel: 1,
    });
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
    expect(component['drawerVisible']()).toBeFalse();
  });

  it('clearFilters resets and removes saved filters', () => {
    sessionStorage.setItem('app-role-list-filters', JSON.stringify({ keyword: 'x' }));
    component.clearFilters();

    expect(sessionStorage.getItem('app-role-list-filters')).toBeNull();
    expect(component['filters']).toEqual({ keyword: null, permissionLevel: null });
  });

  it('add navigates to the new form', () => {
    component.add();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-roles/new']);
  });

  it('view and edit navigate with the role id', () => {
    component.view(ROLES[0]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-roles', 'Admin']);
    component.edit(ROLES[1]);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/app-roles', 'User', 'edit']);
  });

  it('remove deletes when the confirmation is accepted', () => {
    confirmSpy.confirm.and.callFake((c: Confirmation) => {
      c.accept?.();
      return confirmSpy;
    });

    component.remove(ROLES[0], new Event('click'));

    expect(serviceSpy.delete).toHaveBeenCalledWith('Admin');
    // reload after delete
    expect(serviceSpy.query).toHaveBeenCalledTimes(2);
  });

  it('surfaces an error when loading fails', () => {
    serviceSpy.query.and.returnValue(throwError(() => new Error('boom')));
    component.load();
    expect(component['loading']()).toBeFalse();
  });
});
